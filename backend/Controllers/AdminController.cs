using backend.DTOs;
using backend.DTOs.Admin;
using backend.DTOs.Manager;
using backend.DTOs.Error;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IManagerService _managerService;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly IAgentService _agentService;

    public AdminController(
        IUserService userService,
        IManagerService managerService,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        IAgentService agentService)
    {
        _userService = userService;
        _managerService = managerService;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _agentService = agentService;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Create manager
    /// </summary>
    [HttpPost("managers")]
    public async Task<ActionResult<ManagerListItemDTO>> CreateManager([FromBody] CreateManagerRequest request)
    {
        var admin = await _userService.GetUserByIdAsync(long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
        var password = string.IsNullOrWhiteSpace(request.Password) ? "Temp@1234" : request.Password;
        var manager = await _managerService.CreateManagerAsync(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.NationalId,
            request.Email,
            request.WorkId,
            password,
            admin
        );
        var user = manager.User;
        var managerDTO = new ManagerListItemDTO
        {
            Id = $"mgr-{user.Id:D3}",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            NationalId = user.NationalId,
            WorkId = user.WorkId,
            Status = user.Active ? "active" : "inactive",
            AgentsCount = 0,
            LastLogin = user.LastLogin?.ToString(),
            CreatedAt = user.CreatedAt?.ToString("yyyy-MM-dd")
        };
        return CreatedAtAction(nameof(CreateManager), managerDTO);
    }

    /// <summary>
    /// Update user status
    /// </summary>
    [HttpPut("users/{id}/status")]
    public async Task<ActionResult<UserDTO>> UpdateUserStatus(long id, [FromQuery] bool active)
    {
        var updatedUser = await _userService.UpdateUserStatusAsync(id, active);
        var userDTO = new UserDTO
        {
            Id = $"usr-{updatedUser.Id:D3}",
            FirstName = updatedUser.FirstName,
            LastName = updatedUser.LastName,
            PhoneNumber = updatedUser.PhoneNumber,
            NationalId = updatedUser.NationalId,
            Email = updatedUser.Email,
            WorkId = updatedUser.WorkId,
            Role = updatedUser.Role,
            CreatedAt = UserDTO.FormatDate(updatedUser.CreatedAt),
            Active = updatedUser.Active,
            Status = updatedUser.Active ? "active" : "inactive"
        };
        return Ok(userDTO);
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    [HttpPut("users/{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromQuery] string newPassword)
    {
        await _userService.ResetPasswordAsync(id, newPassword);
        return Ok();
    }

    // --- AUDIT LOG ENDPOINTS ---
    [HttpGet("audit-logs")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetAuditLogs([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
        var logs = await _auditLogService.GetLogsByDateRangeAsync(start, end);
        return Ok(logs);
    }

    [HttpGet("audit-logs/paged")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetPaginatedAuditLogs([FromQuery] int page = 0, [FromQuery] int size = 20)
    {
        var logs = await _auditLogService.GetAllLogsPaginatedAsync(page, size);
        return Ok(logs);
    }

    [HttpGet("audit-logs/{id}")]
    public async Task<ActionResult<AuditLogDTO>> GetAuditLogById(long id)
    {
        var log = await _auditLogService.GetLogByIdAsync(id);
        if (log == null) return NotFound();
        return Ok(log);
    }

    [HttpGet("audit-logs/user/{userId}")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetUserAuditLogs(long userId)
    {
        var logs = await _auditLogService.GetLogsByUserIdAsync(userId);
        return Ok(logs);
    }

    [HttpGet("audit-logs/entity")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetEntityAuditLogs([FromQuery] string entityType, [FromQuery] string entityId)
    {
        var logs = await _auditLogService.GetLogsByEntityTypeAndIdAsync(entityType, entityId);
        return Ok(logs);
    }

    [HttpGet("audit-logs/search")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> SearchAuditLogs([FromQuery] string action, [FromQuery] string entityType, [FromQuery] string entityId, [FromQuery] string userRole, [FromQuery] string details, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
        var logs = await _auditLogService.SearchLogsAsync(action, entityType, entityId, userRole, details, start, end);
        return Ok(logs);
    }

    // --- MANAGER ENDPOINTS ---
    [HttpGet("managers")]
    public async Task<ActionResult<IEnumerable<ManagerListItemDTO>>> GetAllManagers()
    {
        var managers = await _managerService.GetAllManagersAsync();
        var managerDTOs = new List<ManagerListItemDTO>();
        foreach (var manager in managers)
        {
            var user = manager.User;
            var agentsCount = (await _agentService.GetAgentsByManagerAsync(manager)).Count();
            managerDTOs.Add(new ManagerListItemDTO
            {
                Id = $"mgr-{user.Id:D3}",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                NationalId = user.NationalId,
                WorkId = user.WorkId,
                Status = user.Active ? "active" : "inactive",
                AgentsCount = agentsCount,
                LastLogin = user.LastLogin?.ToString(),
                CreatedAt = user.CreatedAt?.ToString("yyyy-MM-dd")
            });
        }
        return Ok(managerDTOs);
    }

    [HttpPut("managers/{id}")]
    public async Task<ActionResult<UserDTO>> UpdateManager(long id, [FromBody] Dictionary<string, object> updateRequest)
    {
        var updatedManager = await _managerService.UpdateManagerAsync(id, updateRequest);
        var user = updatedManager.User;
        var updatedManagerDTO = new UserDTO
        {
            Id = $"mgr-{user.Id:D3}",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            NationalId = user.NationalId,
            WorkId = user.WorkId,
            Role = user.Role,
            CreatedAt = UserDTO.FormatDate(user.CreatedAt),
            Active = user.Active
        };
        return Ok(updatedManagerDTO);
    }

    [HttpDelete("managers/{id}")]
    public async Task<IActionResult> DeleteManager(long id)
    {
        await _managerService.DeleteManagerAsync(id);
        return NoContent();
    }

    // --- ADMIN DASHBOARD ENDPOINT ---
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDTO>> GetDashboard()
    {
        var dashboard = await _userService.GetAdminDashboardAsync();
        return Ok(dashboard);
    }

    // --- NOTIFICATION ENDPOINTS ---
    [HttpPost("notifications")]
    public async Task<ActionResult> SendNotification([FromBody] Dictionary<string, string> body)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var sender = await _userService.GetUserByIdAsync(userId);
        var result = await _notificationService.SendNotificationAsync(body, sender);
        return StatusCode(201, result);
    }

    [HttpGet("notifications")]
    public async Task<ActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        var result = await _notificationService.GetNotificationsPagedAsync(page, limit);
        return Ok(result);
    }

    // --- HELPER: TimeAgo ---
    private string FormatTimeAgo(DateTime dateTime)
    {
        var now = DateTime.Now;
        var minutes = (now - dateTime).TotalMinutes;
        var hours = (now - dateTime).TotalHours;
        var days = (now - dateTime).TotalDays;
        if (minutes < 60)
            return $"{(int)minutes} minutes ago";
        else if (hours < 24)
            return $"{(int)hours} hours ago";
        else
            return $"{(int)days} days ago";
    }
}