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
using Microsoft.Extensions.Logging; // Required for ILogger injection

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
                services.AddScoped<IThumbnailRepository, ThumbnailRepository>(); // --- NEW: Register IThumbnailRepository ---

                // Register infrastructure services
                services.AddSingleton<IFileStorageService>(provider =>
                    new AzureBlobStorageService(
                        provider.GetRequiredService<IConfiguration>(), // Pass IConfiguration
                        provider.GetRequiredService<ILogger<AzureBlobStorageService>>() // Pass ILogger
                    )
                );
                services.AddSingleton<IMessageQueueService, AzureServiceBusMessageQueueService>();
                // VideoProcessingService depends on IThumbnailService, so register ThumbnailService first
                services.AddScoped<IThumbnailService, ThumbnailService>(); // --- NEW: Register IThumbnailService ---
                services.AddScoped<IVideoProcessingService, VideoProcessingService>();


                // Register the hosted worker service
                // The TranscodingWorkerService constructor now directly takes IConfiguration
                services.AddHostedService<TranscodingWorkerService>(provider =>
                    new TranscodingWorkerService(
                        provider.GetRequiredService<ILogger<TranscodingWorkerService>>(),
                        provider.GetRequiredService<IServiceProvider>(), // Pass IServiceProvider for scope creation
                        provider.GetRequiredService<IConfiguration>() // Pass IConfiguration directly
                    )
                );

                // FFmpeg and FFprobe paths are now directly accessed in TranscodingWorkerService via IConfiguration
                // The FFmpegSettings class is no longer needed for this approach.
            })
            .Build();

        await builder.RunAsync();
    }
}