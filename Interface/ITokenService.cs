namespace MyBackend.Interface;

public interface ITokenService
{
    string GenerateToken(Guid userId);
    Guid ValidateToken(string token);
}