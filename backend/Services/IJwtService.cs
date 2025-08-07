using backend.Models;
using System.Security.Claims;

namespace backend.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    ClaimsPrincipal? GetPrincipalFromToken(string token);
    bool IsTokenExpired(string token);
}