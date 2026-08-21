using System.ComponentModel.DataAnnotations;

namespace DispatchPal.Api.Features.Auth;

public sealed record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    string Password);