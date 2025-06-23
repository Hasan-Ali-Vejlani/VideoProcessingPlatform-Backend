// VideoProcessingPlatform.Core/Interfaces/IMessageQueueService.cs
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs; // For TranscodingJobMessage

namespace VideoProcessingPlatform.Core.Interfaces
{
    // Interface for abstracting message queue operations.
    public interface IMessageQueueService
    {
        // Publishes a transcoding job message to the queue.
        Task PublishTranscodingJob(TranscodingJobMessage message);

        // Consumes a transcoding job message from the queue.
        // The worker application will call this.
        // Returns null if no message is available within a timeout.
        Task<TranscodingJobMessage?> ConsumeTranscodingJob();

        // Acknowledges that a message has been successfully processed and can be removed from the queue.
        Task AcknowledgeMessage(string messageId);

        // Notifies the queue that a message processing failed and should be retried or moved to a dead-letter queue.
        Task DeadLetterMessage(string messageId, string reason, string description);
    }
}
