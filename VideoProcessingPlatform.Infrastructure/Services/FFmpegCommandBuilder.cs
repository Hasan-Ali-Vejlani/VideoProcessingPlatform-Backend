// VideoProcessingPlatform.Infrastructure/Services/FFmpegCommandBuilder.cs
using VideoProcessingPlatform.Core.Interfaces;
using System;

namespace VideoProcessingPlatform.Infrastructure.Services
{
    // Concrete implementation of IFFmpegCommandBuilder.
    // This class is responsible for constructing FFmpeg command arguments.
    public class FFmpegCommandBuilder : IFFmpegCommandBuilder
    {
        // Builds the complete FFmpeg command arguments template for a given profile.
        // The baseArgsTemplate should contain the core FFmpeg commands, with placeholders
        // for input and output paths (e.g., "-i {inputPath} -c:v libx264 -preset veryfast {outputPath}").
        public string BuildCommand(string resolution, int bitrateKbps, string format, string baseArgsTemplate,
                                   string inputPathPlaceholder = "{inputPath}", string outputPathPlaceholder = "{outputPath}")
        {
            // The logic here can be as simple or as complex as needed.
            // For now, we assume the `baseArgsTemplate` already contains the main logic,
            // and we might just append/replace based on resolution, bitrate, format.
            // In a real-world scenario, this method would be highly sophisticated,
            // building arguments based on detailed profile settings (codecs, presets, etc.).

            // Example: Append resolution and bitrate to the template.
            // This is a simplified example. A robust implementation would parse
            // the template and insert options strategically.
            string finalArgs = baseArgsTemplate;

            // Simple replacements or additions
            if (!finalArgs.Contains("-s") && !string.IsNullOrEmpty(resolution))
            {
                finalArgs += $" -s {resolution}"; // Add resolution if not already present
            }
            if (!finalArgs.Contains("-b:v") && bitrateKbps > 0)
            {
                finalArgs += $" -b:v {bitrateKbps}k"; // Add video bitrate
            }
            // Add format if needed, though usually the output file extension dictates it.
            // For HLS/DASH, there would be more complex flags like -hls_time, -hls_playlist_type, -f hls etc.

            // Ensure placeholders are present for worker replacement
            if (!finalArgs.Contains(inputPathPlaceholder))
            {
                finalArgs = $"-i {inputPathPlaceholder} {finalArgs}"; // Prepend if missing
            }
            if (!finalArgs.Contains(outputPathPlaceholder))
            {
                // This might need smarter placement, typically at the end or before output-specific flags
                finalArgs += $" {outputPathPlaceholder}";
            }

            return finalArgs.Trim(); // Remove leading/trailing spaces
        }

        // Validates if the provided FFmpegArgsTemplate contains the necessary placeholders.
        // A more advanced validation might check for valid FFmpeg commands/syntax.
        public bool ValidateTemplate(string template)
        {
            // Ensure essential placeholders are present.
            // Adjust these placeholders as per your FFmpeg workflow requirements.
            bool containsInput = template.Contains("{inputPath}");
            bool containsOutput = template.Contains("{outputPath}");

            // Add other critical flags you expect, e.g., video codec, audio codec
            // bool containsVideoCodec = template.Contains("-c:v");
            // bool containsAudioCodec = template.Contains("-c:a");

            return containsInput && containsOutput; // && containsVideoCodec && containsAudioCodec;
        }
    }
}
