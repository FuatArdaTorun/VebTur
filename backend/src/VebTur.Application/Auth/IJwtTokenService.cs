using VebTur.Application.Contracts.Auth;

namespace VebTur.Application.Auth;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(Guid userId, string email, IReadOnlyList<string> roles);
}
