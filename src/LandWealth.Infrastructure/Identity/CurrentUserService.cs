using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LandWealth.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LandWealth.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId { get; } = Resolve(httpContextAccessor.HttpContext?.User);
    public bool IsAuthenticated { get; } = httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    private static Guid Resolve(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }
}
