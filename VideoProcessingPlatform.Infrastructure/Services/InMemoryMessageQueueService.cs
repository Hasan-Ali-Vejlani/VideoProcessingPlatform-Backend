//// VideoProcessingPlatform.Infrastructure/Services/InMemoryMessageQueueService.cs
//using VideoProcessingPlatform.Core.DTOs;
//using VideoProcessingPlatform.Core.Interfaces;
//using System;
//using System.Collections.Concurrent; // For ConcurrentQueue
//using System.Threading.Tasks;
//using System.Threading; // For CancellationToken

//namespace VideoProcessingPlatform.Infrastructure.Services
//{
//    // A simplified in-memory implementation of IMessageQueueService for demonstration purposes.
//    // In a real application, this would be replaced by a robust message queue like Azure Service Bus, Kafka, or RabbitMQ.
//    public class InMemoryMessageQueueService : IMessageQueueService
//    {
//        // Using a ConcurrentQueue to simulate a message queue for multiple producers/consumers.
//        // In a real message queue, messages would be persistent and fault-tolerant.
//        private static ConcurrentQueue<Tuple<string, TranscodingJobMessage>> _messageQueue = new ConcurrentQueue<Tuple<string, TranscodingJobMessage>>();

//        // Simple ID counter for messages (not robust for real-world)
//        private static int _messageIdCounter = 0;

//        // Publishes a transcoding job message to the queue.
//        public Task PublishTranscodingJob(TranscodingJobMessage message)
//        {
//            var messageId = Guid.NewGuid().ToString(); // Generate a unique message ID
//            _messageQueue.Enqueue(Tuple.Create(messageId, message));
//            Console.WriteLine($"[InMemoryMessageQueue] Published job message: {messageId} for Job ID: {message.TranscodingJobId}");
//            return Task.CompletedTask;
//        }

//        // Consumes a transcoding job message from the queue.
//        // This simulates a blocking call with a timeout.
//        public Task<TranscodingJobMessage?> ConsumeTranscodingJob()
//        {
//            // In a real scenario, this would involve long-polling or a message receive loop from the actual queue.
//            // For in-memory, we'll try to dequeue immediately.
//            if (_messageQueue.TryDequeue(out var dequeuedItem))
//            {
//                var messageId = dequeuedItem.Item1;
//                var message = dequeuedItem.Item2;
//                Console.WriteLine($"[InMemoryMessageQueue] Consumed job message: {messageId} for Job ID: {message.TranscodingJobId}");
//                // In a real queue, you'd store the messageId for later acknowledgment/dead-lettering.
//                // For this simple demo, it's immediately dequeued.
//                return Task.FromResult<TranscodingJobMessage?>(message);
//            }

//            Console.WriteLine("[InMemoryMessageQueue] No messages available to consume.");
//            return Task.FromResult<TranscodingJobMessage?>(null); // No message available
//        }

//        // Acknowledges that a message has been successfully processed.
//        // For in-memory, this does nothing as messages are immediately dequeued upon consumption.
//        public Task AcknowledgeMessage(string messageId)
//        {
//            Console.WriteLine($"[InMemoryMessageQueue] Acknowledged message: {messageId} (No-op for in-memory queue).");
//            return Task.CompletedTask;
//        }

//        // Notifies that message processing failed.
//        // For in-memory, this does nothing; in a real queue, it might move to a dead-letter queue.
//        public Task DeadLetterMessage(string messageId, string reason, string description)
//        {
//            Console.WriteLine($"[InMemoryMessageQueue] Dead-lettered message: {messageId}. Reason: {reason}. Description: {description} (No-op for in-memory queue).");
//            return Task.CompletedTask;
//        }
//    }
//}
