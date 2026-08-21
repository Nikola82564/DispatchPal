using DispatchPal.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DispatchPal.Api.Infrastructure.Messaging;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IIntegrationEventPublisher publisher,
    ILogger<OutboxPublisherWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingMessagesAsync(stoppingToken);

                await Task.Delay(
                    TimeSpan.FromSeconds(2),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Outbox publisher failed. Retrying in 5 seconds.");

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
        }
    }

    private async Task PublishPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<DispatchPalDbContext>();

        var messages = await dbContext.OutboxMessages
            .Where(message => message.PublishedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(
                    message,
                    cancellationToken);

                message.PublishedAtUtc = DateTimeOffset.UtcNow;
                message.AttemptCount++;
                message.LastError = null;

                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Outbox message {MessageId} marked as published.",
                    message.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.AttemptCount++;
                message.LastError = exception.Message;

                try
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception databaseException)
                {
                    logger.LogError(
                        databaseException,
                        "Failed to save error information for Outbox message {MessageId}.",
                        message.Id);
                }

                logger.LogError(
                    exception,
                    "Failed to publish Outbox message {MessageId}. It will be retried.",
                    message.Id);
            }
        }
    }
}
