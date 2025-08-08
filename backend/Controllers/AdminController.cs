using backend.DTOs;
using backend.DTOs.Admin;
using backend.DTOs.Manager;
using backend.DTOs.Notification;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController(
        IUserService userService,
        IManagerService managerService,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        IAgentService agentService)
    : ControllerBase
{
    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDTO>>> GetAllUsers()
    {
        var users = await userService.GetAllUsersDTOAsync();
        return Ok(users);
    }

    /// <summary>
    /// Create manager
    /// </summary>
    [HttpPost("managers")]
    public async Task<ActionResult<ManagerListItemDTO>> CreateManager([FromBody] CreateManagerRequest request)
    {
        var admin = await userService.GetUserByIdAsync(long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException()));
        if (admin == null)
        {
            return BadRequest("User not found or account is inactive/deleted.");
        }
        
        // Use provided password or default to "Temp@1234" to match Java version
        var password = string.IsNullOrWhiteSpace(request.Password) ? "Temp@1234" : request.Password;
        
        var manager = await managerService.CreateManagerAsync(
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
        var managerDto = new ManagerListItemDTO
        {
            Id = $"mgr-{user.Id:D3}",
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? throw new InvalidOperationException(),
            NationalId = user.NationalId ?? throw new InvalidOperationException(),
            WorkId = user.WorkId,
            Status = user.Active ? "active" : "inactive",
            AgentsCount = 0,
            LastLogin = user.LastLogin.HasValue ? user.LastLogin.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "",
            CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd")
        };
        
        // Return 201 CREATED status to match Java version
        return StatusCode(201, managerDto);
    }

    /// <summary>
    /// Update user status
    /// </summary>
    [HttpPut("users/{id}/status")]
    public async Task<ActionResult<UserDTO>> UpdateUserStatus(long id, [FromQuery] bool active)
    {
        var updatedUser = await userService.UpdateUserStatusAsync(id, active);
        if (updatedUser == null)
        {
            return BadRequest("User not found or account is inactive/deleted.");
        }
        
        var userDto = new UserDTO
        {
            Id = $"usr-{updatedUser.Id:D3}",
            FirstName = updatedUser.FirstName ?? "",
            LastName = updatedUser.LastName ?? "",
            PhoneNumber = updatedUser.PhoneNumber ?? "",
            NationalId = updatedUser.NationalId ?? "",
            Email = updatedUser.Email ?? "",
            WorkId = updatedUser.WorkId ?? "",
            Role = updatedUser.Role.ToString().ToLower(),
            CreatedAt = UserDTO.FormatDate(updatedUser.CreatedAt) ?? "",
            Active = updatedUser.Active,
            Status = updatedUser.Active ? "active" : "inactive"
        };
        return Ok(userDto);
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    [HttpPut("users/{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(long id, [FromQuery] string newPassword)
    {
        var success = await userService.ResetPasswordAsync(id, newPassword);
        if (!success)
        {
            return BadRequest("User not found or account is inactive/deleted.");
        }
        return Ok();
    }

    // --- AUDIT LOG ENDPOINTS ---
    [HttpGet("audit-logs")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetAuditLogs([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
        var logs = await auditLogService.GetLogsByDateRangeAsync(start, end);
        return Ok(logs);
    }

    [HttpGet("audit-logs/paged")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetPaginatedAuditLogs([FromQuery] int page = 0, [FromQuery] int size = 20)
    {
        var logs = await auditLogService.GetAllLogsPaginatedAsync(page, size);
        return Ok(logs);
    }

    [HttpGet("audit-logs/{id}")]
    public async Task<ActionResult<AuditLogDTO>> GetAuditLogById(long id)
    {
        var log = await auditLogService.GetLogByIdAsync(id);
        if (log == null) return NotFound();
        return Ok(log);
    }

    [HttpGet("audit-logs/user/{userId}")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetUserAuditLogs(long userId)
    {
        var logs = await auditLogService.GetLogsByUserIdAsync(userId);
        return Ok(logs);
    }

    [HttpGet("audit-logs/entity")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetEntityAuditLogs([FromQuery] string entityType, [FromQuery] string entityId)
    {
        var logs = await auditLogService.GetLogsByEntityTypeAndIdAsync(entityType, entityId);
        return Ok(logs);
    }

    [HttpGet("audit-logs/search")]
    public async Task<ActionResult<IEnumerable<AuditLogDTO>>> SearchAuditLogs([FromQuery] string action, [FromQuery] string entityType, [FromQuery] string entityId, [FromQuery] string userRole, [FromQuery] string details, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
        
        var filters = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(action)) filters["action"] = action;
        if (!string.IsNullOrEmpty(entityType)) filters["entityType"] = entityType;
        if (!string.IsNullOrEmpty(entityId)) filters["entityId"] = entityId;
        if (!string.IsNullOrEmpty(details)) filters["details"] = details;
        
        var logs = await auditLogService.SearchLogsAsync(filters, start, end);
        return Ok(logs);
    }

    // NEW: Advanced logs endpoint to match Java version
    [HttpGet("logs")]
    public async Task<ActionResult<object>> GetAuditLogsAdvanced(
        [FromQuery] string? level,
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        // Get all logs first (this should ideally be done in the service layer)
        var allLogs = await auditLogService.GetAllLogsAsync();
        
        // Apply filters
        var filteredLogs = allLogs.AsEnumerable();
        
        if (!string.IsNullOrEmpty(level))
        {
            filteredLogs = filteredLogs.Where(l => string.Equals(l.EventType, level, StringComparison.OrdinalIgnoreCase));
        }
        
        if (!string.IsNullOrEmpty(category))
        {
            filteredLogs = filteredLogs.Where(l => string.Equals(l.EntityType, category, StringComparison.OrdinalIgnoreCase));
        }
        
        if (!string.IsNullOrEmpty(search))
        {
            filteredLogs = filteredLogs.Where(l => 
                (l.Details != null && l.Details.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (l.EventType != null && l.EventType.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (l.EntityType != null && l.EntityType.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }
        
        var logsList = filteredLogs.ToList();
        var total = logsList.Count;
        var fromIndex = Math.Min((page - 1) * limit, total);
        var toIndex = Math.Min(fromIndex + limit, total);
        var pagedLogs = logsList.Skip(fromIndex).Take(limit);
        
        // Map to response format
        var logList = pagedLogs.Select(log => new
        {
            id = log.Id.ToString(),
            timestamp = log.Timestamp?.ToString(),
            level = log.EventType,
            category = log.EntityType,
            message = log.EventType + (!string.IsNullOrEmpty(log.EntityType) ? " " + log.EntityType : ""),
            user = log.User?.Email ?? "System",
            ip = ExtractIpFromDetails(log.Details),
            details = log.Details
        }).ToList();
        
        // Calculate stats
        var stats = new
        {
            errorCount = logsList.Count(l => string.Equals(l.EventType, "error", StringComparison.OrdinalIgnoreCase)),
            warningCount = logsList.Count(l => string.Equals(l.EventType, "warning", StringComparison.OrdinalIgnoreCase)),
            infoCount = logsList.Count(l => string.Equals(l.EventType, "info", StringComparison.OrdinalIgnoreCase)),
            successCount = logsList.Count(l => string.Equals(l.EventType, "success", StringComparison.OrdinalIgnoreCase))
        };
        
        var pagination = new
        {
            page,
            limit,
            total
        };
        
        var response = new
        {
            logs = logList,
            stats,
            pagination
        };
        
        return Ok(response);
    }

    // --- MANAGER ENDPOINTS ---
    [HttpGet("managers")]
    public async Task<ActionResult<IEnumerable<ManagerListItemDTO>>> GetAllManagers()
    {
        var managers = await managerService.GetAllManagersAsync();
        var managerDtOs = new List<ManagerListItemDTO>();
        foreach (var manager in managers)
        {
            var user = manager.User;
            var agents = await agentService.GetAgentsByManagerAsync(manager);
            var agentsCount = agents.Count();
            managerDtOs.Add(new ManagerListItemDTO
            {
                Id = $"mgr-{user.Id:D3}",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber ?? throw new InvalidOperationException(),
                NationalId = user.NationalId ?? throw new InvalidOperationException(),
                WorkId = user.WorkId,
                Status = user.Active ? "active" : "inactive",
                AgentsCount = agentsCount,
                LastLogin = user.LastLogin.HasValue ? user.LastLogin.Value.ToString("yyyy-MM-ddTHH:mm:ssZ") : "",
                CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd")
            });
        }
        return Ok(managerDtOs);
    }

    [HttpPut("managers/{id}")]
    public async Task<ActionResult<UserDTO>> UpdateManager(long id, [FromBody] Dictionary<string, object> updateRequest)
    {
        var updatedManager = await managerService.UpdateManagerAsync(id, updateRequest);
        var user = updatedManager.User;
        var agent = user.Agent;
        var updatedManagerDto = new UserDTO
        {
            Id = $"mgr-{user.Id:D3}",
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber ?? "",
            NationalId = user.NationalId ?? "",
            WorkId = user.WorkId ?? "",
            Role = user.Role.ToString().ToLower(),
            CreatedAt = UserDTO.FormatDate(user.CreatedAt) ?? "",
            Active = user.Active,
            Type = agent?.AgentType.ToString().ToLower() ?? "",
            Sector = agent?.Sector ?? "",
            Group = agent?.Group?.Name ?? "",
            IsTeamLeader = agent?.Group?.Leader?.UserId == user.Id,
            Status = user.Active ? "active" : "inactive",
            ClientsCollected = agent?.ClientsCollected ?? 0,
            AttendanceRate = 0
        };
        return Ok(updatedManagerDto);
    }

    [HttpDelete("managers/{id}")]
    public async Task<IActionResult> DeleteManager(long id)
    {
        await managerService.DeleteManagerAsync(id);
        return NoContent();
    }

    // --- ADMIN DASHBOARD ENDPOINT ---
    [HttpGet("dashboard")]
    public async Task<ActionResult<object>> GetDashboard()
    {
        // Build system metrics for the last 6 months
        var systemMetrics = new List<object>();
        var now = DateTime.Now;
        
        for (int i = 5; i >= 0; i--)
        {
            var monthStart = now.AddMonths(-i).Date.AddDays(1 - now.AddMonths(-i).Day);
            var monthName = monthStart.ToString("MMM");
            
            // Get user count for this month (implement in service layer)
            var userCount = await userService.GetUserCountForMonthAsync(monthStart);
            var activityCount = await auditLogService.GetActivityCountForMonthAsync(monthStart);

            systemMetrics.Add(new
            {
                name = monthName,
                users = userCount,
                activity = activityCount
            });
        }
        
        // Get current counts
        var managersCount = (await managerService.GetAllManagersAsync()).Count(); 
        var agentsCount = await userService.GetUserCountByRoleAsync("Agent");
        var activeTodayCount = await auditLogService.GetActiveTodayCountAsync();
        var notificationsSentCount = await notificationService.GetTotalSentCountAsync();
        // Calculate changes (simplified version)
        var managersChange = await CalculateChangeAsync(managersCount, "managers");
        var agentsChange = await CalculateChangeAsync(agentsCount, "agents");
        var activeTodayChange = await CalculateChangeAsync(activeTodayCount, "active_today");
        var notificationsChange = await CalculateChangeAsync(notificationsSentCount, "notifications");
        var userActivity = new
        {
            managers = new { count = managersCount, change = managersChange },
            agents = new { count = agentsCount, change = agentsChange },
            activeToday = new { count = activeTodayCount, change = activeTodayChange },
            notificationsSent = new { count = notificationsSentCount, change = notificationsChange }
        };
        
        var recentActivities = await GetRecentSystemActivitiesAsync();
        
        var dashboard = new
        {
            systemMetrics,
            userActivity,
            recentSystemActivities = recentActivities
        };
        
        return Ok(dashboard);
    }

    // --- NOTIFICATION ENDPOINTS ---
    [HttpPost("notifications")]
    public async Task<ActionResult<object>> SendNotification([FromBody] Dictionary<string, string> body)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var sender = await userService.GetUserByIdAsync(userId);
        if (sender == null)
        {
            return BadRequest("User not found or account is inactive/deleted.");
        }
        
        var title = body.GetValueOrDefault("title", "");
        var message = body.GetValueOrDefault("message", "");
        var recipient = body.GetValueOrDefault("recipient", "All Users");
        var priority = body.GetValueOrDefault("priority", "medium");
        var senderRole = body.GetValueOrDefault("senderRole", "");
        var senderWorkId = body.GetValueOrDefault("senderWorkId", "");
        
        // Validate sender role and workId
        if (!string.IsNullOrEmpty(senderRole) && !string.Equals(senderRole, sender.Role.ToString().ToLower(), StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { error = "Sender role mismatch" });
        }
        
        if (!string.IsNullOrEmpty(senderWorkId) && !string.Equals(senderWorkId, sender.WorkId))
        {
            return StatusCode(403, new { error = "Sender workId mismatch" });
        }
        
        // Send notification logic
        var result = await notificationService.SendNotificationAsync(body, sender);
        
        // Format response to match Java structure
        var response = new
        {
            id = result.Id.ToString(),
            title = title,
            message = message,
            recipient = recipient,
            priority = priority,
            category = body.GetValueOrDefault("category", "system"),
            status = "sent",
            sentAt = result.SentAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            readBy = 0,
            totalRecipients = 1, // Fixed: Use default value since TotalRecipients doesn't exist
            sender = new
            {
                role = sender.Role.ToString().ToLower(),
                workId = sender.WorkId,
                name = $"{sender.FirstName} {sender.LastName}"
            }
        };
        
        return StatusCode(201, response);
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<object>> GetNotifications([FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        var result = await notificationService.GetNotificationsPagedAsync(page, limit);
        
        // Transform to match Java response structure - FIXED
        var notifications = result.Notifications?.Select(n => new
        {
            id = n.Id.ToString(),
            title = n.Title,
            message = n.Message,
            recipient = n.RecipientName ?? "All Users",
            status = n.Status ?? "sent",
            sentAt = n.SentAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            readBy = n.Read ?? false ? 1 : 0, // Fixed: Handle nullable bool
            totalRecipients = 1, // Fixed: Use default value since TotalRecipients doesn't exist
            priority = n.Priority ?? "normal",
            sender = new
            {
                role = n.SenderRole?.ToLower(),
                workId = n.SenderWorkId,
                name = n.SenderName
            }
        }).Cast<object>().ToList() ?? new List<object>();
        
        var stats = new
        {
            totalSent = result.TotalCount,
            thisWeek = result.ThisWeekCount,
            readRate = result.ReadRate
        };
        
        var pagination = new
        {
            page = page,
            limit = limit,
            total = result.TotalCount
        };
        
        var response = new
        {
            notifications,
            stats,
            pagination
        };
        
        return Ok(response);
    }

    // --- HELPER METHODS ---
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
    
    private string? ExtractIpFromDetails(string? details)
    {
        if (string.IsNullOrEmpty(details)) return null;
        
        // Simple regex to extract IP from details string
        var match = System.Text.RegularExpressions.Regex.Match(details, @"ip:(\d+\.\d+\.\d+\.\d+)");
        return match.Success ? match.Groups[1].Value : null;
    }
    
    // These methods should be implemented in your service layer
    private async Task<int> GetUserCountForMonthAsync(DateTime monthStart)
    {
        return await userService.GetUserCountForMonthAsync(monthStart);
    }
    
    private async Task<int> GetActivityCountForMonthAsync(DateTime monthStart)
    {
        return await auditLogService.GetActivityCountForMonthAsync(monthStart);
    }
    
    private async Task<int> GetActiveTodayCountAsync()
    {
        return await auditLogService.GetActiveTodayCountAsync();
    }
    
    private async Task<int> GetNotificationsSentCountAsync()
    {
        return await notificationService.GetTotalSentCountAsync();
    }
    
    private async Task<string> CalculateChangeAsync(int currentCount, string metricType)
    {
        var previousCount = await GetPreviousPeriodCountAsync(metricType);
        var change = currentCount - previousCount;
        return (change >= 0 ? "+" : "") + change;
    }
    
    private async Task<int> GetPreviousPeriodCountAsync(string metricType)
    {
        return await auditLogService.GetPreviousPeriodCountAsync(metricType);
    }
    
    private async Task<List<object>> GetRecentSystemActivitiesAsync()
    {
        var recentLogs = await auditLogService.GetRecentActivitiesAsync(5);
        
        return recentLogs.Select(log => new
        {
            action = $"{log.EventType} {log.EntityType}",
            user = GetUserNameFromLog(log),
            time = FormatTimeAgo(log.Timestamp ?? DateTime.Now)
        }).Cast<object>().ToList();
    }

    // Helper method to get username from audit log
    private string GetUserNameFromLog(backend.Models.AuditLog log)
    {
        if (log.User != null)
        {
            return $"{log.User.FirstName} {log.User.LastName}";
        }
        return "System";
    }
}