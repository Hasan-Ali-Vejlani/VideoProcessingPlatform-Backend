// VideoProcessingPlatform.Worker/Program.cs
using Microsoft.Extensions.Configuration; // For configuration
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using VideoProcessingPlatform.Core.Interfaces;
using VideoProcessingPlatform.Infrastructure.Data;
using VideoProcessingPlatform.Infrastructure.Repositories;
using VideoProcessingPlatform.Infrastructure.Services;
using VideoProcessingPlatform.Worker.Services; // Your worker service

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Configuration (appsettings.json for Worker project)
                var configuration = hostContext.Configuration;

                // Configure DbContext for the worker
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                // Register repositories
                services.AddScoped<IUploadMetadataRepository, UploadMetadataRepository>();
                services.AddScoped<IEncodingProfileRepository, EncodingProfileRepository>();
                services.AddScoped<ITranscodingJobRepository, TranscodingJobRepository>();

                // Register infrastructure services
                services.AddSingleton<IFileStorageService, AzureBlobStorageService>();
                services.AddSingleton<IMessageQueueService, AzureServiceBusMessageQueueService>();
                services.AddScoped<IVideoProcessingService, VideoProcessingService>();

                // Register the hosted worker service
                services.AddHostedService<TranscodingWorkerService>();

                // Add FFmpeg path to configuration for worker
                // Ensure this is present in the Worker's appsettings.json
                services.Configure<FFmpegSettings>(configuration.GetSection("FFmpeg"));
            })
            .Build();

        await builder.RunAsync();
    }
}

// FFmpegSettings class to bind configuration
public class FFmpegSettings
{
    public string Path { get; set; } = "ffmpeg"; // Default value
}
