// VideoProcessingPlatform.Worker/Services/TranscodingWorkerService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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

namespace VideoProcessingPlatform.Worker.Services
{
    public class TranscodingWorkerService : BackgroundService
    {
        private readonly ILogger<TranscodingWorkerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _ffmpegPath;

        public TranscodingWorkerService(ILogger<TranscodingWorkerService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _ffmpegPath = configuration["FFmpeg:Path"] ?? "ffmpeg";
            _logger.LogInformation($"FFmpeg Path configured as: {_ffmpegPath}");
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

                    QueuedMessage<TranscodingJobMessage>? queuedMessage = null;
                    TranscodingJobMessage? jobMessage = null;

                    try
                    {
                        _logger.LogInformation("Attempting to consume message from queue...");
                        queuedMessage = await messageQueueService.ConsumeTranscodingJob();

                        if (queuedMessage != null)
                        {
                            jobMessage = queuedMessage.Content;

                            _logger.LogInformation($"Processing Transcoding Job ID: {jobMessage.TranscodingJobId}");

                            await videoProcessingService.UpdateTranscodingJobProgress(
                                jobMessage.TranscodingJobId, 0, "Processing started.", "InProgress");

                            string localInputPath = "";
                            Stream? sourceVideoStream = null;
                            try
                            {
                                sourceVideoStream = await fileStorageService.RetrieveFile(jobMessage.SourceVideoPath);
                                localInputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_{Path.GetFileName(jobMessage.SourceVideoPath)}");
                                using (var fileStream = new FileStream(localInputPath, FileMode.Create, FileAccess.Write))
                                {
                                    await sourceVideoStream.CopyToAsync(fileStream);
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
                                    // --- FIX: Truncate deadLetterErrorDescription ---
                                    if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);
                                    await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "Source Video Download Failed", errorDescription);
                                }
                                continue;
                            }
                            finally
                            {
                                sourceVideoStream?.Dispose();
                            }

                            string tempOutputDirectory = Path.Combine(Path.GetTempPath(), $"transcode_{jobMessage.TranscodingJobId}");
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

                                process.ErrorDataReceived += (sender, e) =>
                                {
                                    if (!string.IsNullOrEmpty(e.Data))
                                    {
                                        ffmpegErrorOutput.AppendLine(e.Data);
                                    }
                                };

                                process.Start();
                                process.BeginErrorReadLine();
                                await process.WaitForExitAsync(stoppingToken);
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
                                        // --- FIX: Truncate deadLetterErrorDescription here ---
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
                                    // --- FIX: Truncate deadLetterErrorDescription here ---
                                    string errorDescription = $"FFmpeg Exception: {ex.Message}";
                                    if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);
                                    await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "FFmpeg Exception", errorDescription);
                                }
                            }
                            finally
                            {
                                if (File.Exists(localInputPath))
                                {
                                    try { File.Delete(localInputPath); } catch (Exception cleanupEx) { _logger.LogWarning($"Failed to delete temporary input file {localInputPath}: {cleanupEx.Message}"); }
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
                                            renditions.Add(new VideoRenditionDto
                                            {
                                                RenditionType = $"{jobMessage.TargetResolution}_{jobMessage.TargetFormat}",
                                                StoragePath = storedPaths.First(),
                                                IsEncrypted = jobMessage.ApplyDRM,
                                                PlaybackUrl = storedPaths.First()
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
                                                // --- FIX: Truncate deadLetterErrorDescription here ---
                                                if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);
                                                await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "No Renditions Stored", errorDescription);
                                            }
                                        }
                                    }

                                    await videoProcessingService.CompleteTranscodingJob(jobMessage.TranscodingJobId, renditions);
                                    _logger.LogInformation($"Transcoding Job {jobMessage.TranscodingJobId} completed and renditions added.");

                                    if (queuedMessage.RawMessageHandle != null)
                                    {
                                        await messageQueueService.AcknowledgeMessage(queuedMessage.RawMessageHandle);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Failed to upload renditions or complete job for {jobMessage.TranscodingJobId}.");
                                    await videoProcessingService.FailTranscodingJob(jobMessage.TranscodingJobId, $"Failed to store renditions or update job: {ex.Message}");
                                    if (queuedMessage.RawMessageHandle != null)
                                    {
                                        // --- FIX: Truncate deadLetterErrorDescription here ---
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
                                    // --- FIX: Truncate deadLetterErrorDescription here ---
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
                        // --- FIX: Ensure errorDescription is always truncated before dead-lettering ---
                        string errorDescription = $"Unexpected Worker Error: {ex.Message}";
                        if (errorDescription.Length > 4096) errorDescription = errorDescription.Substring(0, 4096);

                        if (queuedMessage?.RawMessageHandle != null)
                        {
                            await messageQueueService.DeadLetterMessage(queuedMessage.RawMessageHandle, "Unexpected Worker Error", errorDescription);
                        }
                        if (jobMessage != null)
                        {
                            await videoProcessingService.FailTranscodingJob(jobMessage.TranscodingJobId, errorDescription); // Also truncate for DB
                        }
                        await Task.Delay(5000, stoppingToken);
                    }
                }
            }
            _logger.LogInformation("Transcoding Worker Service stopped.");
        }
    }
}
