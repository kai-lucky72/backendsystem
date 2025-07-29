using backend.DTOs.Auth;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;

    public AuthController(IUserService userService, IJwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] AuthRequest request)
    {
        var user = await _userService.GetUserByWorkIdAsync(request.WorkId);
        if (user == null || !user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized();
        }
        if (!string.IsNullOrEmpty(request.Role) && !user.Role.ToString().Equals(request.Role, StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized();
        }
        var token = _jwtService.GenerateToken(user);
        var userInfo = new AuthResponse.UserInfo
        {
            Id = $"usr-{user.Id:D3}",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Name = user.FirstName + " " + user.LastName,
            Email = user.Email,
            WorkId = user.WorkId,
            Role = user.Role.ToString().ToLower()
        };
        var response = new AuthResponse
        {
            Token = token,
            User = userInfo
        };
        return Ok(response);
    }
}