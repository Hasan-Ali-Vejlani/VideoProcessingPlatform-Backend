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
            bool containsInput = template.Contains("{inputPath}");
            bool containsOutput = template.Contains("{outputPath}");


            return containsInput && containsOutput;
        }
    }
}
