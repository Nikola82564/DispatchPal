namespace DispatchPal.Api.Domain.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public required string EventType { get; set; }

    public required string RoutingKey { get; set; }

    public required string Payload { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public string? LastError { get; set; }
}