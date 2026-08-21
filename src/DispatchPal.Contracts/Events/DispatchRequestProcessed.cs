namespace DispatchPal.Contracts.Events;

public sealed record DispatchRequestProcessed(
    Guid EventId,
    Guid RequestId,
    Guid CustomerId,
    string CustomerEmail,
    bool Succeeded,
    string ResultMessage,
    DateTimeOffset ProcessedAtUtc);