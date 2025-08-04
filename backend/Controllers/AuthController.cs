using backend.DTOs.Auth;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using backend.DTOs;

namespace backend.Controllers;

/// <summary>
/// Authentication API for login with workId, email, and role
/// 
/// ## How to use JWT Authentication in Swagger:
/// 1. First, call the `/api/auth/login` endpoint with your credentials
/// 2. Copy the `token` from the response
/// 3. Click the 🔒 **Authorize** button at the top of Swagger UI
/// 4. In the popup, enter ONLY the token (without "Bearer" prefix)
/// 5. Click **Authorize** to save
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
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService userService, 
        IAgentService agentService,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _agentService = agentService;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// User login - Authenticates a user with workId, email, and optional role
    /// </summary>
    /// <param name="request">Authentication request containing workId, email, and optional role</param>
    /// <returns>Authentication response with JWT token and user information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody][Required] AuthRequest request)
    {
        // Validate the request model
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("LOGIN REQUEST RECEIVED - workId: {WorkId}, email: {Email}, role: {Role}", 
            request.WorkId, request.Email, request.Role);
        
        try
        {
            // Direct database lookup - matches Java logic exactly
            User? user = await _userService.GetUserByWorkIdAsync(request.WorkId);
            
            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent or inactive user with workId: {WorkId}", request.WorkId);
                return Unauthorized(new { message = "User not found or account is inactive/deleted." });
            }
            
            // Email validation - exact match like Java
            if (!user.Email.Equals(request.Email, StringComparison.Ordinal))
            {
                _logger.LogError("Email mismatch for user {WorkId}: provided={ProvidedEmail}, actual={ActualEmail}", 
                    user.WorkId, request.Email, user.Email);
                return Unauthorized();
            }
            
            // Role validation if provided - matches Java logic exactly
            if (!string.IsNullOrEmpty(request.Role) && 
                !request.Role.Equals(user.Role.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Role mismatch for user {WorkId}: provided={ProvidedRole}, actual={ActualRole}", 
                    user.WorkId, request.Role, user.Role.ToString());
            return Unauthorized();
        }
            
            _logger.LogInformation("Authentication successful for user: {WorkId}", user.WorkId);
            
            // Generate JWT token using existing service
        var token = _jwtService.GenerateToken(user);
            
            // Create the response according to frontend requirements - matches Java exactly
        var userInfo = new AuthResponse.UserInfo
        {
                Id = string.Format("usr-{0:D3}", user.Id), // Matches Java formatting exactly
            FirstName = user.FirstName,
            LastName = user.LastName,
            Name = user.FirstName + " " + user.LastName,
            Email = user.Email,
            WorkId = user.WorkId,
                Role = user.Role.ToString().ToLower()
            };
            
            // Add agent-specific info if the user is an agent - matches Java logic exactly
            if (user.Role == Role.AGENT)
            {
                try
                {
                    var agent = await _agentService.GetAgentByIdAsync(user.Id);
                    if (agent != null)
                    {
                        userInfo.AgentType = agent.AgentType.ToString().ToLower();
                        if (agent.Group != null)
                        {
                            userInfo.GroupName = agent.Group.Name;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to get agent info for user {WorkId}: {Error}", user.WorkId, ex.Message);
                    // Continue without agent info - don't fail the login
                }
            }
            
        var response = new AuthResponse
        {
            Token = token,
            User = userInfo
        };
            
            _logger.LogInformation("Login successful for user: {WorkId}, returning token", user.WorkId);
        return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication error: {Message}", ex.Message);
            return Unauthorized();
        }
    }
 
    /// <summary>
    /// Alternative route matching Java's dual mapping
    /// </summary>
    [HttpPost]
    [Route("/auth/login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> LoginAlternativeRoute([FromBody][Required] AuthRequest request)
    {
        // Delegate to main login method to avoid code duplication
        return await Login(request);
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
            
            // Test with active user
            try
            {
                var activeUser = await _userService.GetUserByWorkIdAsync("ADM001");
                results.Add(new { 
                    workId = "ADM001", 
                    status = "active", 
                    active = activeUser.Active,
                    success = true 
                });
            }
            catch (Exception ex)
            {
                results.Add(new { 
                    workId = "ADM001", 
                    status = "error", 
                    error = ex.Message,
                    success = false 
                });
            }
            
            return Ok(new { 
                message = "Active user validation test completed",
                results = results,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
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
                    WorkId = user.WorkId,
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