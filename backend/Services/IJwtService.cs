using backend.Models;

namespace backend.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
}