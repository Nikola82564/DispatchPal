using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DispatchPal.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class authController (IOptions<JwtOptions> options) : ControllerBase
{
    private readonly JwtOptions _options = options.Value;

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        var emailMatches = string.Equals(
            request.Email.Trim(),
            _options.UserEmail,
            StringComparison.OrdinalIgnoreCase);

        var suppliedPasswordHash = SHA256.HashData(Encoding.UTF8.GetBytes(request.Password));

        var configuredPasswordHash = SHA256.HashData(
           Encoding.UTF8.GetBytes(
               _options.UserPassword));

        var passwordMatches =
            CryptographicOperations.FixedTimeEquals(
                suppliedPasswordHash,
                configuredPasswordHash);

        if (!emailMatches || !passwordMatches)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials.",
                Detail =
                    "The supplied email or password is incorrect.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        var now = DateTimeOffset.UtcNow;

        var expiresAtUtc = now.AddMinutes(_options.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub, _options.UserEmail),

            new Claim(JwtRegisteredClaimNames.Email, _options.UserEmail),

            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.Key));

        var signingCredentials = new SigningCredentials(
            signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse(
            accessToken,
            expiresAtUtc,
            _options.UserEmail));
    }
}