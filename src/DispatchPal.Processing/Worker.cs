using DispatchPal.Contracts.Events;
using DispatchPal.Processing.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace DispatchPal.Processing;

public sealed class Worker(IOptions<RabbitMqOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    private const string ProcessedRoutingKey =
    "dispatch-request.processed";
    private readonly RabbitMqOptions _options = options.Value;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "RabbitMQ is unavailable. Retrying in 5 seconds.");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            ClientProvidedName = "DispatchPal.Processing",
            AutomaticRecoveryEnabled = true,
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);

        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            autoDelete: false,
            exclusive: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await channel.QueueBindAsync(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: _options.RoutingKey,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken
        );

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            logger.LogInformation(
                "RabbitMQ delivered message {DeliveryTag}.",
                eventArgs.DeliveryTag);
            try
            {
                var integrationEvent =
                    JsonSerializer.Deserialize<DispatchRequestCreated>(
                        eventArgs.Body.Span);

                if (integrationEvent is null)
                {
                    throw new JsonException(
                        "Message body was deserialized to null.");
                }

                logger.LogInformation(
                       "Processing DispatchRequest {RequestId}, " +
                       "EventId {EventId}, from {PickupAddress} to " +
                       "{DeliveryAddress}.",
                       integrationEvent.RequestId,
                       integrationEvent.EventId,
                       integrationEvent.PickupAddress,
                       integrationEvent.DeliveryAddress);

                await Task.Delay(
                    TimeSpan.FromSeconds(2),
                    stoppingToken);

                logger.LogInformation(
                       "Finished processing DispatchRequest {RequestId}.",
                       integrationEvent.RequestId);

                var processedEvent = new DispatchRequestProcessed(
                    EventId: Guid.NewGuid(),
                    RequestId: integrationEvent.RequestId,
                    CustomerId: integrationEvent.CustomerId,
                    CustomerEmail: integrationEvent.CustomerEmail,
                    Succeeded: true,
                    ResultMessage: "Dispatch request processed successfully.",
                    ProcessedAtUtc: DateTimeOffset.UtcNow);

                var processedBody =
                    JsonSerializer.SerializeToUtf8Bytes(processedEvent);

                var processedProperties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = processedEvent.EventId.ToString(),
                    Type = nameof(DispatchRequestProcessed)
                };

                await channel.BasicPublishAsync(
                    exchange: _options.ExchangeName,
                    routingKey: ProcessedRoutingKey,
                    mandatory: true,
                    basicProperties: processedProperties,
                    body: processedBody,
                    cancellationToken: stoppingToken);

                logger.LogInformation(
                    "Published DispatchRequestProcessed {EventId} " +
                    "for DispatchRequest {RequestId}.",
                    processedEvent.EventId,
                    processedEvent.RequestId);

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (JsonException exception)
            {
                logger.LogError(
                    exception,
                    "Invalid message received. DeliveryTag: {DeliveryTag}.",
                    eventArgs.DeliveryTag);

                await channel.BasicRejectAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }

        };
            await channel.BasicConsumeAsync(
                queue: _options.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            logger.LogInformation(
                "Listening on queue {QueueName}.",
                 _options.QueueName);

            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                stoppingToken);
    }
}
