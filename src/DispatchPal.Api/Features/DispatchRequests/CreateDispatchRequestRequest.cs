using System.ComponentModel.DataAnnotations;

namespace DispatchPal.Api.Features.DispatchRequests;

public sealed record CreateDispatchRequestRequest(
    [Required]
    Guid CustomerId,

    [Required]
    [StringLength(500)]
    string PickupAddress,

    [Required]
    [StringLength(500)]
    string DeliveryAddress,

    [Required]
    [StringLength(1000)]
    string PackageDescription);