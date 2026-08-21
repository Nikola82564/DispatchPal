using System.ComponentModel.DataAnnotations;

namespace DispatchPal.Api.Features.Customers
{
public sealed record GetCustomersQuery(
    [StringLength(320)]
    string? Search = null,

    [Range(1, int.MaxValue)]
    int Page = 1,

    [Range(1, 100)]
    int PageSize = 10);
}
