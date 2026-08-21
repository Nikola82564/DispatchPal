namespace DispatchPal.Contracts.Events;

public sealed record DispatchRequestCreated(
    Guid EventId,
    Guid RequestId,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string PickupAddress,
    string DeliveryAddress,
    string PackageDescription,
    DateTimeOffset OccurredAtUtc);