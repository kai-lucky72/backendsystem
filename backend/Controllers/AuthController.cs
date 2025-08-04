using backend.DTOs.Auth;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace backend.Controllers;

/// <summary>
/// Authentication API for login with workId, email, and role
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
            User? user = null;
            try 
            {
                user = await _userService.GetUserByWorkIdAsync(request.WorkId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while looking up user with workId: {WorkId}", request.WorkId);
                // User not found or database error - continue to return unauthorized
            }
            
            if (user == null)
            {
                _logger.LogError("User not found with workId: {WorkId}", request.WorkId);
                return Unauthorized();
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
            var userCount = await _userService.GetAllUsersAsync();
            return Ok(new { 
                status = "healthy", 
                userCount = userCount.Count(),
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
}