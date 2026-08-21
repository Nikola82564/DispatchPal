using DispatchPal.Api.Domain.Entities;

namespace DispatchPal.Api.Infrastructure.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken);
}