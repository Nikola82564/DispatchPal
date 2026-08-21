using DispatchPal.Api.Domain;
using DispatchPal.Api.Domain.Entities;
using DispatchPal.Api.Infrastructure.Persistence;
using DispatchPal.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using Npgsql;

namespace DispatchPal.Api.Infrastructure.Messaging;

public sealed class DispatchRequestProcessedConsumer(IOptions<RabbitMqOptions> options,
IServiceScopeFactory scopeFactory,
ILogger<DispatchRequestProcessedConsumer> logger) : BackgroundService
{
    private const string QueueName = "dispatchpal.api";
    private const string RoutingKey = "dispatch-request.processed";
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
            ClientProvidedName = "DispatchPal.Consumer",
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
            queue: QueueName,
            durable: true,
            autoDelete: false,
            exclusive: false,
            arguments: null,
            cancellationToken: stoppingToken
        );

        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: _options.ExchangeName,
            routingKey: RoutingKey,
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

                await using var scope = scopeFactory.CreateAsyncScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<DispatchPalDbContext>();

                var alreadyProcessed = await dbContext.InboxMessages
                .AnyAsync(
                message => message.EventId == integrationEvent.EventId,
                stoppingToken);

                if (alreadyProcessed)
                {
                    logger.LogInformation(
                        "Event {EventId} was already processed.",
                        integrationEvent.EventId);

                    await channel.BasicAckAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    return;
                }

                var dispatchRequest =
                await dbContext.DispatchRequests.SingleOrDefaultAsync(
                request =>
                request.Id == integrationEvent.RequestId,
                stoppingToken);

                if (dispatchRequest is null)
                {
                    logger.LogWarning(
                        "DispatchRequest {RequestId} was not found.",
                        integrationEvent.RequestId);

                    await channel.BasicRejectAsync(
                        deliveryTag: eventArgs.DeliveryTag,
                        requeue: false,
                        cancellationToken: stoppingToken);

                    return;
                }

                dispatchRequest.Status = integrationEvent.Succeeded
                ? DispatchRequestStatus.Completed
                : DispatchRequestStatus.Failed;

                var statusHistory = new DispatchRequestStatusHistory
                {
                    Id = Guid.NewGuid(),
                    DispatchRequestId = dispatchRequest.Id,
                    Status = dispatchRequest.Status,
                    Description = integrationEvent.ResultMessage,
                    ChangedAtUtc = integrationEvent.ProcessedAtUtc
                };

                dbContext.DispatchRequestStatusHistories.Add(statusHistory);

                dbContext.InboxMessages.Add(
                new InboxMessage
                {
                    EventId = integrationEvent.EventId,
                    EventType = nameof(DispatchRequestProcessed),
                    ProcessedAtUtc = DateTimeOffset.UtcNow
                });

                await dbContext.SaveChangesAsync(stoppingToken);

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);

                logger.LogInformation(
                    "DispatchRequest {RequestId} status changed to {Status}.",
                    dispatchRequest.Id,
                    dispatchRequest.Status);
            }

            catch (JsonException exception)
            {
                logger.LogError(
                    exception,
                    "Invalid DispatchRequestProcessed message.");

                await channel.BasicRejectAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
            catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "PK_InboxMessages"
            })
            {
                logger.LogInformation(
                    "Event was concurrently processed by another consumer.");

                await channel.BasicAckAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }

            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to update DispatchRequest status.");

                await channel.BasicNackAsync(
                    deliveryTag: eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: stoppingToken);
            }
        };
        await channel.BasicConsumeAsync(
           queue: QueueName,
           autoAck: false,
           consumer: consumer,
           cancellationToken: stoppingToken);

        logger.LogInformation(
            "Listening for processed events on queue {QueueName}.",
            QueueName);

        await Task.Delay(
            Timeout.InfiniteTimeSpan,
            stoppingToken);
    }
}
