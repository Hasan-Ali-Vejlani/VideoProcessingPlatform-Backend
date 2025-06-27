// VideoProcessingPlatform.Infrastructure/Services/AzureServiceBusMessageQueueService.cs
using VideoProcessingPlatform.Core.DTOs;
using VideoProcessingPlatform.Core.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VideoProcessingPlatform.Infrastructure.Services
{
    public class AzureServiceBusMessageQueueService : IMessageQueueService
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _sender;
        private readonly ServiceBusReceiver _receiver;
        private readonly string _queueName;
        private readonly ILogger<AzureServiceBusMessageQueueService> _logger;

        public AzureServiceBusMessageQueueService(IConfiguration configuration, ILogger<AzureServiceBusMessageQueueService> logger)
        {
            _logger = logger;

            var connectionString = configuration.GetConnectionString("AzureServiceBusConnection");
            _queueName = configuration["AzureServiceBus:QueueName"] ?? "transcoding-jobs-queue";

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("AzureServiceBusConnection string is not configured.");
            }
            if (string.IsNullOrEmpty(_queueName))
            {
                throw new InvalidOperationException("AzureServiceBus:QueueName is not configured.");
            }

            _logger.LogInformation($"Connecting to Azure Service Bus Queue: {_queueName}");

            try
            {
                _client = new ServiceBusClient(connectionString);
                _sender = _client.CreateSender(_queueName);
                _receiver = _client.CreateReceiver(_queueName, new ServiceBusReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock });

                _logger.LogInformation($"Successfully connected to Azure Service Bus Queue '{_queueName}'.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Azure Service Bus or create sender/receiver.");
                throw;
            }
        }

        public async Task PublishTranscodingJob(TranscodingJobMessage message)
        {
            try
            {
                var body = JsonSerializer.Serialize(message);
                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(body))
                {
                    MessageId = message.TranscodingJobId.ToString(),
                    ContentType = "application/json"
                };

                await _sender.SendMessageAsync(serviceBusMessage);
                _logger.LogInformation($"[AzureServiceBus] Published job message for Job ID: {message.TranscodingJobId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AzureServiceBus] Failed to publish message for Job ID: {message.TranscodingJobId}");
                throw;
            }
        }

        public async Task<QueuedMessage<TranscodingJobMessage>?> ConsumeTranscodingJob()
        {
            try
            {
                ServiceBusReceivedMessage? receivedMessage = await _receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));

                if (receivedMessage == null)
                {
                    _logger.LogDebug("[AzureServiceBus] No messages available to consume.");
                    return null;
                }

                var messageString = Encoding.UTF8.GetString(receivedMessage.Body.ToArray());
                TranscodingJobMessage? messageContent = null;
                try
                {
                    messageContent = JsonSerializer.Deserialize<TranscodingJobMessage>(messageString);
                    if (messageContent == null)
                    {
                        _logger.LogError($"[AzureServiceBus] Failed to deserialize message content. Marking as dead-letter. MessageId: {receivedMessage.MessageId}");
                        await _receiver.DeadLetterMessageAsync(receivedMessage, "Deserialization Failed", "Message content could not be deserialized into TranscodingJobMessage.").ConfigureAwait(false);
                        return null;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"[AzureServiceBus] JSON deserialization error for message. MessageId: {receivedMessage.MessageId}");
                    await _receiver.DeadLetterMessageAsync(receivedMessage, "JSON Deserialization Error", ex.Message).ConfigureAwait(false);
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[AzureServiceBus] Unexpected error during message deserialization. MessageId: {receivedMessage.MessageId}");
                    await _receiver.DeadLetterMessageAsync(receivedMessage, "Unexpected Deserialization Error", ex.Message).ConfigureAwait(false);
                    return null;
                }

                _logger.LogInformation($"[AzureServiceBus] Consumed job message for Job ID: {messageContent.TranscodingJobId} (LockToken: {receivedMessage.LockToken})");

                // Pass the actual ServiceBusReceivedMessage object as the raw handle
                return new QueuedMessage<TranscodingJobMessage>(messageContent, receivedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AzureServiceBus] Error consuming message from queue.");
                throw;
            }
        }

        public async Task AcknowledgeMessage(object rawMessageHandle)
        {
            if (rawMessageHandle is ServiceBusReceivedMessage message)
            {
                try
                {
                    await _receiver.CompleteMessageAsync(message);
                    _logger.LogInformation($"[AzureServiceBus] Acknowledged message with LockToken: {message.LockToken}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[AzureServiceBus] Error completing message with LockToken: {message.LockToken}");
                    throw;
                }
            }
            else
            {
                _logger.LogError($"[AzureServiceBus] AcknowledgeMessage received invalid rawMessageHandle type: {rawMessageHandle?.GetType().Name ?? "null"}");
                throw new ArgumentException("Invalid message handle provided for acknowledgment.", nameof(rawMessageHandle));
            }
        }

        public async Task DeadLetterMessage(object rawMessageHandle, string reason, string description)
        {
            if (rawMessageHandle is ServiceBusReceivedMessage message)
            {
                try
                {
                    await _receiver.DeadLetterMessageAsync(message, reason, description);
                    _logger.LogWarning($"[AzureServiceBus] Dead-lettered message with LockToken: {message.LockToken}. Reason: {reason}. Description: {description}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[AzureServiceBus] Error dead-lettering message with LockToken: {message.LockToken}");
                    throw;
                }
            }
            else
            {
                _logger.LogError($"[AzureServiceBus] DeadLetterMessage received invalid rawMessageHandle type: {rawMessageHandle?.GetType().Name ?? "null"}");
                throw new ArgumentException("Invalid message handle provided for dead-lettering.", nameof(rawMessageHandle));
            }
        }

        public async void Dispose()
        {
            _logger.LogInformation("Disposing Azure Service Bus client resources.");
            try
            {
                if (_sender != null) await _sender.DisposeAsync();
                if (_receiver != null) await _receiver.DisposeAsync();
                if (_client != null) await _client.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Azure Service Bus client disposal.");
            }
        }
    }
}
