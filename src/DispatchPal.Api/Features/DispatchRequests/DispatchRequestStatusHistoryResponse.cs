using DispatchPal.Api.Domain.Entities;

namespace DispatchPal.Api.Features.DispatchRequests;

public sealed record DispatchRequestStatusHistoryResponse(
    Guid Id,
    DispatchRequestStatus Status,
    string Description,
    DateTimeOffset ChangedAtUtc);