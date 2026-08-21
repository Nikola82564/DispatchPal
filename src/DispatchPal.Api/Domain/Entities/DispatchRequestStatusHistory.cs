using DispatchPal.Api.Domain;

namespace DispatchPal.Api.Domain.Entities;

public sealed class DispatchRequestStatusHistory
{
    public Guid Id { get; set; }

    public Guid DispatchRequestId { get; set; }

    public DispatchRequestStatus Status { get; set; }

    public required string Description { get; set; }

    public DateTimeOffset ChangedAtUtc { get; set; }

    public DispatchRequest DispatchRequest { get; set; } = null!;
}