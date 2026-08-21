using System.ComponentModel.DataAnnotations;

namespace DispatchPal.Api.Features.Customers;

public sealed record CreateCustomerRequest(
    [Required]
    [StringLength(200)]
    string Name,

    [Required]
    [EmailAddress]
    [StringLength(320)]
    string Email,

    [StringLength(30)]
    string? Phone);