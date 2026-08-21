using System.ComponentModel.DataAnnotations;

namespace DispatchPal.Api.Features.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public required string Issuer { get; init; }

    [Required]
    public required string Audience { get; init; }

    [Required]
    [MinLength(32)]
    public required string Key { get; init; }

    [Range(1, 1440)]
    public int ExpirationMinutes { get; init; } = 60;

    [Required]
    [EmailAddress]
    public required string UserEmail { get; init; }

    [Required]
    public required string UserPassword { get; init; }
}