// VideoProcessingPlatform.Core/Interfaces/IMessageQueueService.cs
using System; // For IDisposable
using System.Threading.Tasks;
using VideoProcessingPlatform.Core.DTOs; // For TranscodingJobMessage

namespace VideoProcessingPlatform.Core.Interfaces
{
    // A wrapper DTO to include the message content and a raw message handle for the underlying queue.
    public class QueuedMessage<T> where T : class
    {
        public T Content { get; }
        // This will hold the ServiceBusReceivedMessage directly for Azure Service Bus.
        public object RawMessageHandle { get; }

        public QueuedMessage(T content, object rawMessageHandle)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            RawMessageHandle = rawMessageHandle ?? throw new ArgumentNullException(nameof(rawMessageHandle));
        }
    }

    // Interface for abstracting message queue operations.
    public interface IMessageQueueService : IDisposable
    {
        // Publishes a transcoding job message to the queue.
        Task PublishTranscodingJob(TranscodingJobMessage message);

        // Consumes a transcoding job message from the queue.
        // Returns a QueuedMessage<TranscodingJobMessage> if a message is available, null otherwise.
        Task<QueuedMessage<TranscodingJobMessage>?> ConsumeTranscodingJob();

        // Acknowledges that a message has been successfully processed and can be removed from the queue.
        // It takes the raw message handle returned by ConsumeTranscodingJob.
        Task AcknowledgeMessage(object rawMessageHandle);

        // Notifies the queue that a message processing failed and should be retried or moved to a dead-letter queue.
        // It takes the raw message handle returned by ConsumeTranscodingJob.
        Task DeadLetterMessage(object rawMessageHandle, string reason, string description);
    }
}
