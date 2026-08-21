using DispatchPal.Api.Domain.Entities;

namespace DispatchPal.Api.Features.DispatchRequests;

public sealed record DispatchRequestResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string PickupAddress,
    string DeliveryAddress,
    string PackageDescription,
    DispatchRequestStatus Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<DispatchRequestStatusHistoryResponse> StatusHistory);