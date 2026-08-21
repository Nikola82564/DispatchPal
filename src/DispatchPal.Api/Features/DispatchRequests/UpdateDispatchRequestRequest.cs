using System.ComponentModel.DataAnnotations;

namespace DispatchPal.Api.Features.DispatchRequests;

public sealed record UpdateDispatchRequestRequest(
    [Required]
    [StringLength(500)]
    string PickupAddress,

    [Required]
    [StringLength(500)]
    string DeliveryAddress,

    [Required]
    [StringLength(1000)]
    string PackageDescription);