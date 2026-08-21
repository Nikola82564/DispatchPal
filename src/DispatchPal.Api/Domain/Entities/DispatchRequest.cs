namespace DispatchPal.Api.Domain.Entities;

public sealed class DispatchRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    public required string PickupAddress { get; set; }

    public required string DeliveryAddress { get; set; }

    public required string PackageDescription { get; set; }

    public DispatchRequestStatus Status { get; set; } =
        DispatchRequestStatus.Pending;

    public DateTimeOffset CreatedAtUtc { get; set; } =
        DateTimeOffset.UtcNow;

    public Customer Customer { get; set; } = null!;

    public ICollection<DispatchRequestStatusHistory> StatusHistory { get; set; } =
        new List<DispatchRequestStatusHistory>();
}
