namespace DispatchPal.Api.Domain.Entities;

public sealed class InboxMessage
{
    public Guid EventId { get; set; }

    public required string EventType { get; set; }

    public DateTimeOffset ProcessedAtUtc { get; set; }
}