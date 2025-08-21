using backend.DTOs.Auth;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using backend.DTOs;
using Microsoft.Extensions.Configuration;

namespace backend.Controllers;

/// <summary>
/// Authentication API for login with workId, email, and role
/// 
/// ## How to use JWT Authentication in Swagger:
/// 1. First, call the `/api/auth/login` endpoint with your credentials
/// 2. Copy the `token` from the response
/// 3. Click the 🔒 **Authorize** button at the top of Swagger UI
/// 4. In the popup, enter ONLY the token (without "Bearer" prefix)
/// 5: Click **Authorize** to save
/// 6. Now you can access all protected endpoints!
/// 
/// ## Example:
/// - Token from login: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`
/// - Enter in Swagger: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` (no "Bearer")
/// - Swagger automatically adds "Bearer " prefix
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAgentService _agentService;
    private readonly IJwtService _jwtService;
	private readonly IExternalAuthService _externalAuthService;
    private readonly ILogger<AuthController> _logger;
	private readonly IConfiguration _configuration;

    public AuthController(
        IUserService userService, 
        IAgentService agentService,
        IJwtService jwtService,
		IExternalAuthService externalAuthService,
		ILogger<AuthController> logger,
		IConfiguration configuration)
    {
        _userService = userService;
        _agentService = agentService;
        _jwtService = jwtService;
		_externalAuthService = externalAuthService;
        _logger = logger;
		_configuration = configuration;
    }

    /// <summary>
	/// User login - Delegates to external auth with phone number and password
    /// </summary>
	/// <param name="request">Authentication request containing phone number and password</param>
    /// <returns>Authentication response with JWT token and user information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody][Required] AuthRequest request)
    {
		if (!ModelState.IsValid) return BadRequest(ModelState);
		_logger.LogInformation("LOGIN REQUEST RECEIVED - phone: {Phone}", request.PhoneNumber);

		try
		{
			// 1) System Admin allow-list bypass
			var allowListUsers = _configuration.GetSection("AdminAllowList:Users").GetChildren();
			foreach (var entry in allowListUsers)
			{
				var phone = entry["Phone"]?.Trim();
				var password = entry["Password"];
				if (!string.IsNullOrEmpty(phone) && string.Equals(phone, request.PhoneNumber) && string.Equals(password, request.Password))
				{
					var email = $"{request.PhoneNumber}@system.local";
					var adminUser = await _userService.GetUserByEmailAsync(email)
						?? await _userService.CreateShadowUserAsync("System", "Admin", email, request.PhoneNumber, Role.ADMIN);
					adminUser.Role = Role.ADMIN;
					adminUser.PhoneNumber = request.PhoneNumber;
					adminUser.LastLogin = DateTime.UtcNow;
					await _userService.UpdateUserAsync(adminUser);

					var adminToken = _jwtService.GenerateToken(adminUser);
					var adminInfo = new AuthResponse.UserInfo
					{
						Id = adminUser.Id.ToString(),
						Name = adminUser.FirstName + " " + adminUser.LastName,
						Email = adminUser.Email,
						Role = adminUser.Role.ToString().ToLower()
					};
					return Ok(new AuthResponse { Token = adminToken, User = adminInfo });
				}
			}

			// 2) External authentication for regular users
			var external = await _externalAuthService.AuthenticateAsync(request.PhoneNumber, request.Password);
			if (external == null)
			{
				return Unauthorized(new { message = "User not found or invalid credentials." });
			}

			var mappedRole = MapEmployeeTypeToRole(external.EmployeeTypes);
			var names = (external.Names ?? string.Empty).Trim();
			var firstName = names;
			var lastName = string.Empty;
			var spaceIdx = names.IndexOf(' ');
			if (spaceIdx > 0)
			{
				firstName = names[..spaceIdx];
				lastName = names[(spaceIdx + 1)..];
			}

			// Get or create shadow user locally
			var user = await _userService.GetUserByEmailAsync(external.Email)
				?? await _userService.CreateShadowUserAsync(firstName, lastName, external.Email, request.PhoneNumber, mappedRole, workId: $"EXT-{external.Id}");

			// Ensure role and phone are up to date (admin allow-list by phone)
			var allowList = HttpContext.RequestServices.GetService<string[]>() ?? Array.Empty<string>();
			var isSystemAdmin = allowList.Contains(request.PhoneNumber);
			user.Role = isSystemAdmin ? Role.ADMIN : mappedRole;
			user.PhoneNumber = request.PhoneNumber;
			user.LastLogin = DateTime.UtcNow;
			await _userService.UpdateUserAsync(user);

			// Ensure domain profile exists (JIT upsert)
			if (user.Role == Role.MANAGER && user.Manager == null)
			{
				// Persist minimal manager row; CreatedBy not enforced for external users
				user.Manager = new Manager { UserId = user.Id };
			}
			if (user.Role == Role.AGENT && user.Agent == null)
			{
				// Allow ManagerId to be assigned later by an admin/manager; set defaults
				user.Agent = new Agent { UserId = user.Id, AgentType = AgentType.INDIVIDUAL, Sector = "General" };
			}
			await _userService.UpdateUserAsync(user);

        var token = _jwtService.GenerateToken(user);
            
        var userInfo = new AuthResponse.UserInfo
        {
				Id = user.Id.ToString(),
            Name = user.FirstName + " " + user.LastName,
            Email = user.Email,
                Role = user.Role.ToString().ToLower()
            };
			if (user.Role == Role.AGENT && user.Agent != null)
			{
				userInfo.AgentType = user.Agent.AgentType.ToString().ToLower();
				if (user.Agent.AgentType == AgentType.SALES && user.Agent.Group != null)
				{
					userInfo.GroupName = user.Agent.Group.Name;
				}
			}

			return Ok(new AuthResponse { Token = token, User = userInfo });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error: {Message}", ex.Message);
            return Unauthorized();
        }
    }
 
    [HttpPost]
    [Route("/auth/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginAlternativeRoute([FromBody][Required] AuthRequest request)
    {
        return await Login(request);
    }

	private static Role MapEmployeeTypeToRole(string employeeTypes)
	{
		var normalized = (employeeTypes ?? string.Empty).Trim().ToLowerInvariant();
		return normalized switch
		{
			"commercial" => Role.MANAGER,
			"sales agent" => Role.AGENT,
			_ => Role.AGENT
		};
    }

    /// <summary>
    /// Health check endpoint to test database connectivity
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<ActionResult> HealthCheck()
    {
        try
        {
            var allUsers = await _userService.GetAllUsersAsync();
            var activeUsers = await _userService.GetActiveUsersAsync();
            return Ok(new { 
                status = "healthy", 
                totalUsers = allUsers.Count(),
                activeUsers = activeUsers.Count(),
                inactiveUsers = allUsers.Count() - activeUsers.Count(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                status = "unhealthy", 
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Test endpoint to verify active/inactive user validation
    /// </summary>
    [HttpGet("test-active-validation")]
    [AllowAnonymous]
    public async Task<ActionResult> TestActiveValidation()
    {
        try
        {
            var results = new List<object>();
			// Keep minimal test stub (no WorkId anymore)
			results.Add(new { message = "ok" });
            return Ok(new { 
                message = "Active user validation test completed",
                results = results,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
			return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refresh JWT token for persistent login
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Validate the current token
            var principal = _jwtService.GetPrincipalFromToken(request.Token);
            if (principal == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            // Check if token is expired
            if (_jwtService.IsTokenExpired(request.Token))
            {
                return Unauthorized(new { message = "Token has expired" });
            }

            // Get user ID from token
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token claims" });
            }

            // Get user from database
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found or account is inactive/deleted." });
            }

            // Check if user is still active
            if (!user.Active)
            {
                return Unauthorized(new { message = "User account is inactive/deleted." });
            }

            // Generate new token
            var newToken = _jwtService.GenerateToken(user);

            return Ok(new AuthResponseDto
            {
                Success = true,
                Token = newToken,
                Message = "Token refreshed successfully",
                User = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = user.Role,
                    Active = user.Active
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { message = "Internal server error during token refresh" });
        }
    }
}
