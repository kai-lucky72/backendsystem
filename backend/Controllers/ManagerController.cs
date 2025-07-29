using backend.DTOs;
using backend.DTOs.Agent;
using backend.DTOs.Group;
using backend.DTOs.Manager;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/manager")]
[Authorize(Roles = "Admin,Manager")]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;
    private readonly IAgentService _agentService;
    private readonly IGroupService _groupService;
    private readonly IAttendanceTimeframeService _attendanceTimeframeService;
    private readonly IUserService _userService;
    private readonly IClientService _clientService;
    private readonly INotificationService _notificationService;

    public ManagerController(
        IManagerService managerService,
        IAgentService agentService,
        IGroupService groupService,
        IAttendanceTimeframeService attendanceTimeframeService,
        IUserService userService,
        IClientService clientService,
        INotificationService notificationService)
    {
        _managerService = managerService;
        _agentService = agentService;
        _groupService = groupService;
        _attendanceTimeframeService = attendanceTimeframeService;
        _userService = userService;
        _clientService = clientService;
        _notificationService = notificationService;
    }

    // Example endpoint: Create agent
    [HttpPost("agents")]
    public async Task<ActionResult> CreateAgent([FromBody] CreateAgentRequest request)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var manager = await _managerService.GetManagerByIdAsync(userId);
        var password = string.IsNullOrWhiteSpace(request.Password) ? "Temp@1234" : request.Password;
        var agent = await _agentService.CreateAgentAsync(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.NationalId,
            request.Email,
            request.WorkId,
            password,
            manager,
            request.GetAgentType(),
            request.GetSectorOrDefault()
        );
        // ... map to DTO and return
        return Ok();
    }

    // ... (other endpoints: agents, groups, dashboard, notifications, etc. will be mapped similarly)
}