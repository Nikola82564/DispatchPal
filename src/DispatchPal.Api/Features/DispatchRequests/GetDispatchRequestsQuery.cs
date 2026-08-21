using DispatchPal.Api.Domain;
using DispatchPal.Api.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace DispatchPal.Api.Features.DispatchRequests;

public sealed record GetDispatchRequestsQuery(
    Guid? CustomerId = null,
    DispatchRequestStatus? Status = null,

    [StringLength(500)]
    string? Search = null,

    [Range(1, int.MaxValue)]
    int Page = 1,

    [Range(1, 100)]
    int PageSize = 10);