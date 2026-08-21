using DispatchPal.Api.Domain;
using DispatchPal.Api.Domain.Entities;

namespace DispatchPal.Api.Features.DispatchRequests;

public sealed record DispatchRequestListItemResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string PickupAddress,
    string DeliveryAddress,
    string PackageDescription,
    DispatchRequestStatus Status,
    DateTimeOffset CreatedAtUtc);