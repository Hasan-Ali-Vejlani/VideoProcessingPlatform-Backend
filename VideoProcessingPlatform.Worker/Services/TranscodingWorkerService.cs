// VideoProcessingPlatform.Worker/Services/TranscodingWorkerService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration; // IMPORTANT: Ensure this is included for IConfiguration
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; // Required for parsing FFprobe output (new addition)

namespace VideoProcessingPlatform.Worker.Services
{
    public class TranscodingWorkerService : BackgroundService
    {
        private readonly ILogger<TranscodingWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _ffmpegPath;
        private readonly string _ffprobePath;

        public TranscodingWorkerService(ILogger<TranscodingWorkerService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _ffmpegPath = configuration["FFmpeg:Path"] ?? "ffmpeg";
            _ffprobePath = configuration["FFprobe:Path"] ?? "ffprobe";
            _logger.LogInformation($"FFmpeg Path configured as: {_ffmpegPath}");
            _logger.LogInformation($"FFprobe Path configured as: {_ffprobePath}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Transcoding Worker Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                    var videoProcessingService = scope.ServiceProvider.GetRequiredService<IVideoProcessingService>();
                    var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
                    var thumbnailService = scope.ServiceProvider.GetRequiredService<IThumbnailService>();

                    QueuedMessage<TranscodingJobMessage>? queuedMessage = null;
                    TranscodingJobMessage? jobMessage = null;

                    string localInputPath = "";
                    string tempOutputDirectory = "";

                    try
                    {
                        _logger.LogInformation("Attempting to consume message from queue...");
                        // --- FIX: Use ConsumeTranscodingJob() as defined in IMessageQueueService ---
                        queuedMessage = await messageQueueService.ConsumeTranscodingJob();

                        if (queuedMessage != null) // Removed redundant `&& queuedMessage.MessageBody != null` as Content is non-nullable
                        {
                            // --- FIX: Use .Content property as defined in QueuedMessage<T> ---
                            jobMessage = queuedMessage.Content;

                            _logger.LogInformation($"Processing Transcoding Job ID: {jobMessage.TranscodingJobId}");

                            await videoProcessingService.UpdateTranscodingJobProgress(
                                jobMessage.TranscodingJobId, 0, "Processing started.", "InProgress");

                            Stream? sourceVideoStream = null;
                            try
                            {
                                sourceVideoStream = await fileStorageService.RetrieveFile(jobMessage.SourceVideoPath);
                                localInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{Path.GetFileName(jobMessage.SourceVideoPath)}");
                                using (var fileStream = new FileStream(localInputPath, FileMode.Create, FileAccess.Write))
                                {
                                    await sourceVideoStream.CopyToAsync(fileStream, stoppingToken);
                                }
                                _logger.LogInformation($"Downloaded source video to: {localInputPath}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to download source video for job {jobMessage.TranscodingJobId}.");
                                await videoProcessingService.FailTranscodingJob(jobMessage.TranscodingJobId, $"Failed to download source video: {ex.Message}");
                                if (queuedMessage.RawMessageHandle != null)
                                {
                                    string errorDescription = $"Source Video Download Failed: {ex.Message}";
                                    if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);
                                    await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "Source Video Download Failed", errorDescription);
                                }
                                continue;
                            }
                            finally
                            {
                                sourceVideoStream?.Dispose();
                            }

                            tempOutputDirectory = Path.Combine(Path.GetTempPath(), $"transcode_{jobMessage.TranscodingJobId}");
                            Directory.CreateDirectory(tempOutputDirectory);
                            string outputFileName = $"{jobMessage.TranscodingJobId}_{jobMessage.TargetResolution.Replace("x", "p")}.{jobMessage.TargetFormat}";
                            string localOutputPath = Path.Combine(tempOutputDirectory, outputFileName);

                            string ffmpegArgs = jobMessage.FFmpegArgsTemplate
                                .Replace("{inputPath}", $"\"{localInputPath}\"")
                                .Replace("{outputPath}", $"\"{localOutputPath}\"")
                                .Replace("{resolution}", jobMessage.TargetResolution)
                                .Replace("{bitrate}", jobMessage.TargetBitrateKbps.ToString());

                            _logger.LogInformation($"Executing FFmpeg with args: {ffmpegArgs}");

                            bool ffmpegSuccess = false;
                            StringBuilder ffmpegOutput = new StringBuilder();
                            StringBuilder ffmpegErrorOutput = new StringBuilder();

                            try
                            {
                                var process = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = _ffmpegPath,
                                        Arguments = ffmpegArgs,
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true,
                                        UseShellExecute = false,
                                        CreateNoWindow = true,
                                    },
                                    EnableRaisingEvents = true
                                };

                                process.OutputDataReceived += (sender, e) => { if (e.Data != null) ffmpegOutput.AppendLine(e.Data); };
                                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) ffmpegErrorOutput.AppendLine(e.Data); };

                                process.Start();
                                process.BeginOutputReadLine();
                                process.BeginErrorReadLine();
                                await process.WaitForExitAsync(stoppingToken);
                                process.CancelOutputRead();
                                process.CancelErrorRead();

                                if (process.ExitCode == 0)
                                {
                                    _logger.LogInformation($"FFmpeg process completed successfully for Job {jobMessage.TranscodingJobId}.");
                                    ffmpegSuccess = true;
                                }
                                else
                                {
                                    string fullError = ffmpegErrorOutput.ToString();
                                    _logger.LogError($"FFmpeg process failed for Job {jobMessage.TranscodingJobId} with exit code {process.ExitCode}. Error: {fullError}");
                                    await videoProcessingService.FailTranscodingJob(jobMessage.TranscodingJobId, $"FFmpeg failed: {fullError}");
                                    if (queuedMessage.RawMessageHandle != null)
                                    {
                                        string errorDescription = $"FFmpeg Execution Failed: {fullError}";
                                        if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);
                                        await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "FFmpeg Execution Failed", errorDescription);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error during FFmpeg execution for Job {jobMessage.TranscodingJobId}.");
                                await videoProcessingService.FailTranscodingJob(jobMessage.TranscodingJobId, $"FFmpeg execution error: {ex.Message}");
                                if (queuedMessage.RawMessageHandle != null)
                                {
                                    string errorDescription = $"FFmpeg Exception: {ex.Message}";
                                    if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);
                                    await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "FFmpeg Exception", errorDescription);
                                }
                            }

                            if (ffmpegSuccess && File.Exists(localOutputPath))
                            {
                                List<VideoRenditionDto> renditions = new List<VideoRenditionDto>();
                                try
                                {
                                    using (var outputStream = new FileStream(localOutputPath, FileMode.Open, FileAccess.Read))
                                    {
                                        var storedPaths = await fileStorageService.StoreRenditions(
                                            jobMessage.TranscodingJobId,
                                            new Dictionary<string, Stream> { { $"{jobMessage.TargetResolution}_{jobMessage.TargetFormat}", outputStream } }
                                        );

                                        if (storedPaths.Any())
                                        {
                                            string renditionTypeString = $"{jobMessage.TargetResolution.Replace("x", "p")}_{jobMessage.TargetFormat}".ToLower();
                                            renditions.Add(new VideoRenditionDto
                                            {
                                                Id = Guid.NewGuid(),
                                                RenditionType = renditionTypeString,
                                                StoragePath = storedPaths.First(),
                                                IsEncrypted = jobMessage.ApplyDRM,
                                                PlaybackUrl = storedPaths.First(),
                                                Resolution = jobMessage.TargetResolution,
                                                BitrateKbps = jobMessage.TargetBitrateKbps
                                            });
                                            _logger.LogInformation($"Uploaded transcoded rendition to: {storedPaths.First()}");
                                        }
                                        else
                                        {
                                            _logger.LogWarning($"No renditions stored for job {jobMessage.TranscodingJobId} despite FFmpeg success.");
                                            await videoProcessingService.FailTranscodingJob(jobMessage.TranscodingJobId, "FFmpeg successful, but no renditions stored.");
                                            if (queuedMessage.RawMessageHandle != null)
                                            {
                                                string errorDescription = "No Renditions Stored: Output file existed but storage service returned no paths.";
                                                if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);
                                                await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "No Renditions Stored", errorDescription);
                                            }
                                        }
                                    }

                                    _logger.LogInformation($"Generating thumbnails for video ID: {jobMessage.UploadMetadataId} (Job ID: {jobMessage.TranscodingJobId})");

                                    // --- FIX: Pass stoppingToken to GetVideoDurationAsync ---
                                    double videoDurationSeconds = await GetVideoDurationAsync(localInputPath, stoppingToken);
                                    _logger.LogInformation($"Detected video duration: {videoDurationSeconds} seconds for {localInputPath}.");

                                    if (videoDurationSeconds > 0)
                                    {
                                        int numberOfThumbnails = 5;
                                        for (int i = 0; i < numberOfThumbnails; i++)
                                        {
                                            double timestamp = videoDurationSeconds * (0.1 + i * 0.2);
                                            int timestampSeconds = (int)Math.Round(timestamp);

                                            // --- FIX: Pass stoppingToken to GenerateThumbnailAsync ---
                                            byte[] thumbnailData = await GenerateThumbnailAsync(localInputPath, timestampSeconds, stoppingToken);
                                            if (thumbnailData != null && thumbnailData.Length > 0)
                                            {
                                                string thumbnailStoragePath = await fileStorageService.StoreThumbnail(jobMessage.UploadMetadataId, thumbnailData, i);
                                                bool isDefault = (i == 0);
                                                await thumbnailService.AddThumbnailMetadataAsync(jobMessage.UploadMetadataId, thumbnailStoragePath, timestampSeconds, i, isDefault);
                                                _logger.LogInformation($"Generated and stored thumbnail {i} for video {jobMessage.UploadMetadataId} at {timestampSeconds}s.");
                                            }
                                            else
                                            {
                                                _logger.LogWarning($"Failed to generate thumbnail {i} for video {jobMessage.UploadMetadataId} at {timestampSeconds}s. Thumbnail data was empty.");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning($"Video duration could not be determined or was zero for {localInputPath}. Skipping thumbnail generation.");
                                    }

                                    await videoProcessingService.CompleteTranscodingJob(jobMessage.TranscodingJobId, renditions);
                                    _logger.LogInformation($"Transcoding Job {jobMessage.TranscodingJobId} completed and renditions added.");

                                    if (queuedMessage.RawMessageHandle != null)
                                    {
                                        // --- FIX: Use AcknowledgeMessage as defined in IMessageQueueService ---
                                        await messageQueueService.AcknowledgeMessage(queuedMessage.RawMessageHandle);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Failed to upload renditions or complete job for {jobMessage.TranscodingJobId}.");
                                    await videoProcessingService.FailTranscodingJob(jobMessage.TranscodingJobId, $"Failed to store renditions or update job: {ex.Message}");
                                    if (queuedMessage.RawMessageHandle != null)
                                    {
                                        string errorDescription = $"Rendition Storage/Completion Failed: {ex.Message}";
                                        if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);
                                        await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "Rendition Storage/Completion Failed", errorDescription);
                                    }
                                }
                                finally
                                {
                                    if (Directory.Exists(tempOutputDirectory))
                                    {
                                        try { Directory.Delete(tempOutputDirectory, true); } catch (Exception cleanupEx) { _logger.LogWarning($"Failed to delete temporary output directory {tempOutputDirectory}: {cleanupEx.Message}"); }
                                    }
                                }
                            }
                            else if (ffmpegSuccess && !File.Exists(localOutputPath))
                            {
                                _logger.LogError($"FFmpeg reported success but output file not found at {localOutputPath} for Job {jobMessage.TranscodingJobId}.");
                                await videoProcessingService.FailTranscodingJob(jobMessage.TranscodingJobId, "FFmpeg succeeded but output file was not found.");
                                if (queuedMessage.RawMessageHandle != null)
                                {
                                    string errorDescription = "FFmpeg Output Missing: FFmpeg exited successfully but no output file was found.";
                                    if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);
                                    await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "FFmpeg Output Missing", errorDescription);
                                }
                            }
                        }
                        else
                        {
                            await Task.Delay(1000, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Unexpected error processing transcoding job.");
                        string errorDescription = $"Unexpected Worker Error: {ex.Message}";
                        if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);

                        if (queuedMessage?.RawMessageHandle != null)
                        {
                            await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "Unexpected Worker Error", errorDescription);
                        }
                        if (jobMessage != null)
                        {
                            string dbErrorMessage = errorDescription.Length > 1000 ? errorDescription.Substring(0, 1000) : errorDescription;
                            await videoProcessingService.FailTranscodingJob(jobMessage.TranscodingJobId, dbErrorMessage);
                        }
                        await Task.Delay(5000, stoppingToken);
                    }
                    finally
                    {
                        if (File.Exists(localInputPath))
                        {
                            try { File.Delete(localInputPath); } catch (Exception cleanupEx) { _logger.LogWarning($"Failed to delete temporary input file {localInputPath}: {cleanupEx.Message}"); }
                        }
                    }
                }
            }
            _logger.LogInformation("Transcoding Worker Service stopped.");
        }

        // --- NEW HELPER METHOD: GetVideoDurationAsync using FFprobe ---
        // --- FIX: Added CancellationToken parameter ---
        private async Task<double> GetVideoDurationAsync(string videoFilePath, CancellationToken cancellationToken)
        {
            var processInfo = new ProcessStartInfo(_ffprobePath, $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoFilePath}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync(cancellationToken); // Pass cancellationToken
                await process.WaitForExitAsync(cancellationToken); // Pass cancellationToken

                if (process.ExitCode == 0)
                {
                    if (double.TryParse(output.Trim(), out double duration))
                    {
                        return duration;
                    }
                    _logger.LogWarning($"FFprobe output could not be parsed as duration: '{output.Trim()}'");
                }
                else
                {
                    string error = await process.StandardError.ReadToEndAsync(); // Cancellation token not always directly supported here
                    _logger.LogError($"FFprobe failed with exit code {process.ExitCode}. Error: {error}");
                }
            }
            return 0;
        }

        // --- NEW HELPER METHOD: GenerateThumbnailAsync using FFmpeg ---
        // --- FIX: Added CancellationToken parameter ---
        private async Task<byte[]> GenerateThumbnailAsync(string videoFilePath, int timestampSeconds, CancellationToken cancellationToken)
        {
            string ffmpegArgs = $"-i \"{videoFilePath}\" -ss {timestampSeconds} -vframes 1 -f image2 -c:v mjpeg -q:v 2 -s 320x180 -f image2pipe pipe:1";

            var processInfo = new ProcessStartInfo(_ffmpegPath, ffmpegArgs)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();

                    using (var memoryStream = new MemoryStream())
                    {
                        await process.StandardOutput.BaseStream.CopyToAsync(memoryStream, cancellationToken); // Pass cancellationToken
                        await process.WaitForExitAsync(cancellationToken); // Pass cancellationToken

                        if (process.ExitCode == 0)
                        {
                            return memoryStream.ToArray();
                        }
                        else
                        {
                            string error = await process.StandardError.ReadToEndAsync(); // Cancellation token not always directly supported here
                            _logger.LogError($"FFmpeg thumbnail generation failed (exit code {process.ExitCode}) for {videoFilePath} at {timestampSeconds}s. Error: {error}");
                            return Array.Empty<byte>();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception during thumbnail generation for {videoFilePath} at {timestampSeconds}s.");
                return Array.Empty<byte>();
            }
        }
    }
}
