using LandWealth.Domain.Entities;

namespace LandWealth.Application.Common.Interfaces;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    AccessToken Create(User user);
}
