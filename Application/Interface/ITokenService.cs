using System.Security.Claims;

namespace MyBackend.Interface;

public interface ITokenService
{
    string GenerateToken(Guid userId, bool esConsultor);
    ClaimsPrincipal? ValidateToken(string token);
}