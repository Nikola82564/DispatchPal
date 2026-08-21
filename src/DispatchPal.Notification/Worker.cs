using System.Text.Json;
using DispatchPal.Contracts.Events;
using DispatchPal.Notification.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace DispatchPal.Notification;

public sealed class Worker(IOptions<RabbitMqOptions> options, ILogger<Worker> logger) : BackgroundService
{
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
            ClientProvidedName = "DispatchPal.Notification",
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
            try
            {
                var integrationEvent = JsonSerializer.Deserialize<DispatchRequestProcessed>(
                    eventArgs.Body.Span);

                if (integrationEvent is null)
                {
                    throw new JsonException(
                        "Message body was deserialized to null.");
                }

                logger.LogInformation(
                    "Sending notification to {CustomerEmail}: " +
                    "DispatchRequest {RequestId} processed. Result: {Result}.",
                    integrationEvent.CustomerEmail,
                    integrationEvent.RequestId,
                    integrationEvent.ResultMessage);

                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);

                logger.LogInformation(
                    "Notification sent for DispatchRequest {RequestId}.",
                    integrationEvent.RequestId);
            }
            catch(JsonException exception)
            {
                logger.LogError(
                    exception,
                    "Notification failed for message {DeliveryTag}.",
                    eventArgs.DeliveryTag);

                await channel.BasicNackAsync(
                     deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        logger.LogInformation(
            "Listening on queue {QueueName}.",
            _options.QueueName);

        await Task.Delay(
            Timeout.InfiniteTimeSpan,
            stoppingToken);
    }
}
