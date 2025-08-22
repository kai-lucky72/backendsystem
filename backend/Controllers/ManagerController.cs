using backend.DTOs;
using backend.DTOs.Agent;
using backend.DTOs.Manager;
using backend.DTOs.Error;
using backend.DTOs.Group;
using backend.DTOs.Notification;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace backend.Controllers;

/// <summary>
/// Manager Operations - APIs for manager operations including agent and group management
/// </summary>
[ApiController]
[Route("api/manager")]
[Authorize(Roles = "admin,manager")]
public class ManagerController(
    IManagerService managerService,
    IAgentService agentService,
    IGroupService groupService,
    IAttendanceTimeframeService attendanceTimeframeService,
    IUserService userService,
    INotificationService notificationService)
    : ControllerBase
{
    /// <summary>
    /// Create agent - Disabled (users are provisioned via external login)
    /// </summary>
    [HttpPost("agents")]
    public Task<ActionResult> CreateAgent([FromBody] CreateAgentRequest _)
        => Task.FromResult<ActionResult>(StatusCode(403, new ApiErrorResponse
        {
            Status = 403,
            Message = "Agent creation is disabled. Users are provisioned via external login.",
            Timestamp = DateTime.UtcNow
        }));

    /// <summary>
    /// Get all agents - Retrieves all agents under the manager's supervision with their detailed information
    /// </summary>
    [HttpGet("agents")]
    [ProducesResponseType(typeof(List<AgentListItemDTO>), 200)]
    public async Task<ActionResult<List<AgentListItemDTO>>> GetAgents()
    {
        var agents = await agentService.GetAllAgentsAsync();
        
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var today = DateTime.UtcNow;

        var agentDto = new List<AgentListItemDTO>();
        
        foreach (var agent in agents)
        {
            var formattedId = $"agt-{agent.UserId:000}";
            var groupName = agent.Group?.Name ?? "";
            var isTeamLeader = agent.Group?.Leader?.UserId == agent.UserId;
            var status = agent.User.Active ? "active" : "inactive";
            
            var attendances = await agentService.GetAttendanceByAgentAndDateRangeAsync(agent, thirtyDaysAgo, today);
            var attendanceRate = (int)Math.Round((attendances.Count() / 30.0) * 100);

            agentDto.Add(new AgentListItemDTO
            {
                Id = formattedId,
                FirstName = agent.User.FirstName,
                LastName = agent.User.LastName,
                Email = agent.User.Email,
                PhoneNumber = agent.User.PhoneNumber ?? throw new InvalidOperationException(),
                NationalId = agent.User.NationalId ?? "",
                Type = agent.AgentType.ToString().ToLower(),
                Group = groupName,
                IsTeamLeader = isTeamLeader,
                Status = status,
                ClientsCollected = 0,
                AttendanceRate = attendanceRate,
                CreatedAt = agent.User.CreatedAt.ToString("yyyy-MM-dd") ?? ""
            });
        }

        return Ok(agentDto);
    }

    /// <summary>
    /// Update agent - Updates an existing agent's information
    /// </summary>
    [HttpPut("agents/{id}")]
    [ProducesResponseType(typeof(UserDTO), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserDTO>> UpdateAgent(long id, [FromBody] Dictionary<string, object> updateRequest)
    {
        // Any manager can update any agent (no ownership model)
        var agent = await agentService.GetAgentByIdAsync(id);
        // Update agent with the provided fields
        var updatedAgent = await agentService.UpdateAgentAsync(id, updateRequest);

        // Get additional data for the response
        var formattedId = $"agt-{updatedAgent.UserId:000}";
        var isTeamLeader = updatedAgent.Group?.Leader?.UserId == updatedAgent.UserId;
        var groupName = updatedAgent.Group?.Name ?? "";

        // Calculate metrics
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var today = DateTime.UtcNow;

        var attendances = await agentService.GetAttendanceByAgentAndDateRangeAsync(updatedAgent, thirtyDaysAgo, today);
        var attendanceRate = (int)Math.Round((attendances.Count() / 30.0) * 100);

        var updatedAgentDto = new UserDTO
        {
            Id = formattedId,
            FirstName = updatedAgent.User.FirstName,
            LastName = updatedAgent.User.LastName,
            Email = updatedAgent.User.Email,
            PhoneNumber = updatedAgent.User.PhoneNumber ?? throw new InvalidOperationException(),
            NationalId = updatedAgent.User.NationalId ?? "",
            Role = updatedAgent.User.Role.ToString().ToLower(),
            CreatedAt = updatedAgent.User.CreatedAt.ToString("yyyy-MM-dd") ?? "",
            Active = updatedAgent.User.Active,
            Type = updatedAgent.AgentType.ToString().ToLower(),
            Sector = updatedAgent.Sector,
            Group = groupName,
            IsTeamLeader = isTeamLeader,
            Status = updatedAgent.User.Active ? "active" : "inactive",
            ClientsCollected = 0,
            AttendanceRate = attendanceRate
        };

        return Ok(updatedAgentDto);
    }

    /// <summary>
    /// Delete agent - Deletes an agent
    /// </summary>
    [HttpDelete("agents/{id}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> DeleteAgent(long id)
    {
        // Any manager can delete any agent (no ownership model)
        await agentService.DeleteAgentAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Get all groups - Fetches a list of all groups managed by the authenticated manager
    /// </summary>
    [HttpGet("groups")]
    [ProducesResponseType(typeof(List<Dictionary<string, object>>), 200)]
    public async Task<ActionResult<List<Dictionary<string, object>>>> GetGroups()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        var groups = await groupService.GetGroupsByManagerAsync(manager);

        var result = groups.Select(group => new Dictionary<string, object>
        {
            ["id"] = $"group-{group.Id:000}",
            ["name"] = group.Name,
            ["teamLeader"] = group.Leader != null ? new Dictionary<string, object?>
            {
                ["id"] = $"agt-{group.Leader.UserId:000}",
                ["name"] = $"{group.Leader.User.FirstName} {group.Leader.User.LastName}",
                
            } : null,
            ["agents"] = group.Agents.Select(agent => new Dictionary<string, object?>
            {
                ["id"] = $"agt-{agent.UserId:000}",
                ["name"] = $"{agent.User.FirstName} {agent.User.LastName}",
                
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Create group - Creates a new group. Only the name is required.
    /// </summary>
    [HttpPost("groups")]
    [ProducesResponseType(typeof(GroupDetailDTO), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<ActionResult> CreateGroup([FromBody] Dictionary<string, string> body)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);

        if (!body.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { status = 400, message = "Group name is required" });
        }

        // Check for duplicate group name
        var existingGroups = await groupService.GetGroupsByManagerAsync(manager);
        if (existingGroups.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { status = 409, message = "Group name already exists for this manager" });
        }

        var group = await groupService.CreateGroupAsync(name, manager);
        return StatusCode(201, MapGroupToDetailDtoAsync(group));
    }

    /// <summary>
    /// Update group - Updates a group's details, such as its name, description, or team leader.
    /// </summary>
    [HttpPut("groups/{groupId}")]
    [ProducesResponseType(typeof(GroupDetailDTO), 200)]
    public async Task<ActionResult<GroupDetailDTO>> UpdateGroup(long groupId, [FromBody] Dictionary<string, string> body)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        var group = await groupService.GetGroupByIdAsync(groupId);

        if (group.Manager.UserId != manager.UserId)
        {
            return Forbid();
        }

        if (body.TryGetValue("name", out var name))
            group.Name = name;
        if (body.TryGetValue("description", out var description))
            group.Description = description;
        if (body.TryGetValue("teamLeaderId", out var teamLeaderId))
        {
            var leaderId = long.Parse(System.Text.RegularExpressions.Regex.Replace(teamLeaderId, @"[^0-9]", ""));
            var leader = await agentService.GetAgentByIdAsync(leaderId);
            group.Leader = leader;
        }

        var updated = await groupService.SaveGroupAsync(group);
        return Ok(MapGroupToDetailDtoAsync(updated));
    }

    /// <summary>
    /// Delete group - Deletes a group.
    /// </summary>
    [HttpDelete("groups/{groupId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> DeleteGroup(long groupId)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        var group = await groupService.GetGroupByIdAsync(groupId);

        if (group.Manager.UserId != manager.UserId)
        {
            return Forbid();
        }

        await groupService.DeleteGroupAsync(groupId);
        return NoContent();
    }

    /// <summary>
    /// Add agents to group - Adds one or more agents to a group. Only SALES agents not already in a group are eligible.
    /// </summary>
    [HttpPost("groups/{groupId}/agents")]
    public async Task<ActionResult> AddAgentsToGroup(long groupId, [FromBody] Dictionary<string, List<string>> body)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        var group = await groupService.GetGroupByIdAsync(groupId);

        if (group.Manager.UserId != manager.UserId)
        {
            return StatusCode(403, new { error = "Forbidden: Group does not belong to manager" });
        }

        var agentIds = body.GetValueOrDefault("agentIds", new List<string>());
        var errors = new List<string>();

        foreach (var agentIdStr in agentIds)
        {
            var agentId = long.Parse(System.Text.RegularExpressions.Regex.Replace(agentIdStr, @"[^0-9]", ""));
            var agent = await agentService.GetAgentByIdAsync(agentId);

            if ((int)agent.AgentType != (int)AgentType.SALES)
            {
                errors.Add($"Agent {agent.User.FirstName} {agent.User.LastName} is not a SALES agent");
                continue;
            }

            if (agent.Group != null && agent.Group.Id != groupId)
            {
                errors.Add($"Agent {agent.User.FirstName} {agent.User.LastName} is already assigned to another group");
                continue;
            }

            await groupService.AddAgentToGroupAsync(groupId, agentId);
        }

        var updated = await groupService.GetGroupByIdAsync(groupId);
        var response = new Dictionary<string, object>
        {
            ["id"] = updated.Id,
            ["name"] = updated.Name,
            ["teamLeader"] = updated.Leader == null ? null : new Dictionary<string, object?>
            {
                ["id"] = updated.Leader.UserId,
                ["firstName"] = updated.Leader.User.FirstName,
                ["lastName"] = updated.Leader.User.LastName
            },
            ["agents"] = updated.Agents.Select(agent => new Dictionary<string, object?>
            {
                ["id"] = agent.UserId,
                ["firstName"] = agent.User.FirstName,
                ["lastName"] = agent.User.LastName,
                ["email"] = agent.User.Email,
                ["phoneNumber"] = agent.User.PhoneNumber ?? throw new InvalidOperationException(),
                ["nationalId"] = agent.User.NationalId ?? throw new InvalidOperationException(),
                ["type"] = agent.AgentType.ToString().ToLower(),
                ["sector"] = agent.Sector,
                ["status"] = agent.User.Active ? "active" : "inactive",
                ["clientsCollected"] = agent.ClientsCollected,
                ["attendanceRate"] = 0
            }).ToList()
        };

        if (errors.Any())
        {
            response["errors"] = errors;
        }

        return Ok(response);
    }

    /// <summary>
    /// Assign group leader - Assigns a leader to a group. Only SALES agents in the group are eligible.
    /// </summary>
    [HttpPut("groups/{groupId}/leader")]
    public async Task<ActionResult> AssignGroupLeader(long groupId, [FromQuery] long agentId)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        var group = await groupService.GetGroupByIdAsync(groupId);

        if (group.Manager.UserId != manager.UserId)
        {
            return StatusCode(403, new { error = "Forbidden: Group does not belong to manager" });
        }

        var agent = await agentService.GetAgentByIdAsync(agentId);

        if ((int)agent.AgentType != (int)AgentType.SALES)
        {
            return BadRequest(new { error = "Only SALES agents can be assigned as group leader" });
        }

        if (agent.Group == null || agent.Group.Id != groupId)
        {
            return BadRequest(new { error = "Agent must be a member of the group to be assigned as leader" });
        }

        var updatedGroup = await groupService.AssignLeaderAsync(groupId, agentId);

        var response = new Dictionary<string, object>
        {
            ["id"] = updatedGroup.Id,
            ["name"] = updatedGroup.Name,
            ["teamLeader"] = updatedGroup.Leader == null ? null : new Dictionary<string, object?>
            {
                ["id"] = updatedGroup.Leader.UserId,
                ["firstName"] = updatedGroup.Leader.User.FirstName,
                ["lastName"] = updatedGroup.Leader.User.LastName,
                
            },
            ["agents"] = updatedGroup.Agents.Select(a => new Dictionary<string, object?>
            {
                ["id"] = a.UserId,
                ["firstName"] = a.User.FirstName,
                ["lastName"] = a.User.LastName,
                
                ["email"] = a.User.Email,
                ["phoneNumber"] = a.User.PhoneNumber ?? throw new InvalidOperationException(),
                ["nationalId"] = a.User.NationalId ?? "",
                ["type"] = a.AgentType.ToString().ToLower(),
                ["sector"] = a.Sector,
                ["status"] = a.User.Active ? "active" : "inactive",
                ["clientsCollected"] = a.ClientsCollected,
                ["attendanceRate"] = 0
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Add agent to group - Adds an agent to a group
    /// </summary>
    [HttpPut("groups/{groupId}/agents")]
    [ProducesResponseType(typeof(GroupDTO), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GroupDTO>> AddAgentToGroup(long groupId, [FromQuery] long agentId)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);

        // Verify group belongs to this manager
        var group = await groupService.GetGroupByIdAsync(groupId);
        if (group.Manager.UserId != manager.UserId)
        {
            return Forbid();
        }

        var updatedGroup = await groupService.AddAgentToGroupAsync(groupId, agentId);

        // Map to DTO
        var dto = new GroupDTO
        {
            Id = updatedGroup.Id,
            Name = updatedGroup.Name,
            Agents = updatedGroup.Agents.Select(agent =>
            {
                var formattedId = $"agt-{agent.UserId:000}";
                var groupName = agent.Group?.Name ?? "";
                var isTeamLeader = agent.Group?.Leader?.UserId == agent.UserId;
                var status = agent.User.Active ? "active" : "inactive";

                return new UserDTO
                {
                    Id = formattedId,
                    FirstName = agent.User.FirstName,
                    LastName = agent.User.LastName,
                    PhoneNumber = agent.User.PhoneNumber ?? throw new InvalidOperationException(),
                    NationalId = agent.User.NationalId ?? throw new InvalidOperationException(),
                    Email = agent.User.Email,
                    Role = agent.User.Role.ToString().ToLower(),
                    CreatedAt = agent.User.CreatedAt.ToString("yyyy-MM-dd") ?? "",
                    Active = agent.User.Active,
                    Type = agent.AgentType.ToString().ToLower(),
                    Sector = agent.Sector,
                    Group = groupName,
                    Status = status,
                    ClientsCollected = 0,
                    AttendanceRate = 0,
                    IsTeamLeader = isTeamLeader
                };
            }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Remove agent from group - Removes an agent from a group. After removal, agent is eligible for another group.
    /// </summary>
    [HttpDelete("groups/{groupId}/agents/{agentId}")]
    public async Task<ActionResult> RemoveAgentFromGroup(long groupId, long agentId)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);

        // Verify group belongs to this manager
        var group = await groupService.GetGroupByIdAsync(groupId);
        if (group.Manager.UserId != manager.UserId)
        {
            return StatusCode(403, new { error = "Forbidden: Group does not belong to manager" });
        }

        try
        {
            var updatedGroup = await groupService.RemoveAgentFromGroupAsync(groupId, agentId);

            var response = new Dictionary<string, object>
            {
                ["id"] = updatedGroup.Id,
                ["name"] = updatedGroup.Name,
                ["teamLeader"] = updatedGroup.Leader == null ? null : new Dictionary<string, object?>
                {
                    ["id"] = updatedGroup.Leader.UserId,
                    ["firstName"] = updatedGroup.Leader.User.FirstName,
                    ["lastName"] = updatedGroup.Leader.User.LastName
                },
                ["agents"] = updatedGroup.Agents.Select(agent => new Dictionary<string, object?>
                {
                    ["id"] = agent.UserId,
                    ["firstName"] = agent.User.FirstName,
                    ["lastName"] = agent.User.LastName,
                    ["email"] = agent.User.Email,
                    ["phoneNumber"] = agent.User.PhoneNumber ?? throw new InvalidOperationException(),
                    ["nationalId"] = agent.User.NationalId ?? throw new InvalidOperationException(),
                    ["type"] = agent.AgentType.ToString().ToLower(),
                    ["sector"] = agent.Sector,
                    ["status"] = agent.User.Active ? "active" : "inactive",
                    ["clientsCollected"] = agent.ClientsCollected,
                    ["attendanceRate"] = 0
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get group agents - Retrieves all agents of a group
    /// </summary>
    [HttpGet("groups/{groupId}/agents")]
    public async Task<ActionResult<Dictionary<string, object>>> GetGroupAgents(long groupId)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        var group = await groupService.GetGroupByIdAsync(groupId);

        if (group.Manager.UserId != manager.UserId)
        {
            return Forbid();
        }

        var response = new Dictionary<string, object>
        {
            ["id"] = group.Id,
            ["name"] = group.Name,
            ["teamLeader"] = group.Leader == null ? null : new Dictionary<string, object?>
            {
                ["id"] = group.Leader.UserId,
                ["firstName"] = group.Leader.User.FirstName,
                ["lastName"] = group.Leader.User.LastName
            },
            ["agents"] = group.Agents.Select(agent => new Dictionary<string, object?>
            {
                ["id"] = agent.UserId,
                ["firstName"] = agent.User.FirstName,
                ["lastName"] = agent.User.LastName,
                ["email"] = agent.User.Email,
                ["phoneNumber"] = agent.User.PhoneNumber ?? throw new InvalidOperationException(),
                ["nationalId"] = agent.User.NationalId ?? "",
                ["type"] = agent.AgentType.ToString().ToLower(),
                ["sector"] = agent.Sector,
                ["status"] = agent.User.Active ? "active" : "inactive",
                ["clientsCollected"] = agent.ClientsCollected,
                ["attendanceRate"] = 0
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Get agent performance - For sales agents, retrieves group average performance. For individual agents, retrieve individual performance.
    /// </summary>
    [HttpGet("agents/{agentId}/performance")]
    [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<Dictionary<string, object>>> GetSalesAgentPerformance(
        long agentId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);

        // Get the agent and verify it belongs to this manager
        var agent = await agentService.GetAgentByIdAsync(agentId);
        if (agent.Manager.UserId != manager.UserId)
        {
            return Forbid();
        }

        // Default to today if dates not provided
        var start = startDate ?? DateTime.Today;
        var end = endDate ?? DateTime.Today;

        var startDateTime = start.Date;
        var endDateTime = end.Date.AddDays(1).AddSeconds(-1);

        // If agent is SALES type, return group performance
        if ((int)agent.AgentType == (int)AgentType.SALES && agent.Group != null)
        {
            var groupPerformance = await agentService.GetGroupPerformanceAsync(agent.Group, startDateTime, endDateTime);
            return Ok(groupPerformance);
        }
        // If an agent is INDIVIDUAL type or not in a group, return individual performance
        else
        {
            // Get attendance records
            var attendances = await agentService.GetAttendanceByAgentAndDateRangeAsync(agent, startDateTime, endDateTime);

            // Clients removed - no need to track client count

            return Ok(new Dictionary<string, object>
            {
                ["agent"] = new UserDTO
                {
                    Id = $"agt-{agent.UserId:000}",
                    Email = agent.User.Email,
                    
                    FirstName = agent.User.FirstName,
                    LastName = agent.User.LastName,
                    Role = agent.User.Role.ToString().ToLower(),
                    Type = agent.AgentType.ToString().ToLower(),
                    Status = agent.User.Active ? "active" : "inactive"
                },
                ["attendanceCount"] = attendances.Count(),
                ["clientsCollected"] = 0,
                ["startDate"] = start.ToString("yyyy-MM-dd"),
                ["endDate"] = end.ToString("yyyy-MM-dd")
            });
        }
    }

    /// <summary>
    /// Get manager dashboard - Fetches aggregate data for the manager's dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ManagerDashboardDTO), 200)]
    public async Task<ActionResult<ManagerDashboardDTO>> GetDashboard()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        var agents = await agentService.GetAllAgentsAsync();
        
        var startOfToday = DateTime.Today;
        var endOfToday = DateTime.Today.AddDays(1).AddSeconds(-1);

        var presentCount = 0;
        var absentCount = 0;
        var presentAgents = new List<ManagerDashboardDTO.PresentAgent>();
        // Clients removed - no need to track total clients
        var individualPerformance = new List<ManagerDashboardDTO.IndividualPerformanceItem>();

        foreach (var agent in agents)
        {
            var todayAttendance = await agentService.GetAttendanceByAgentAndDateRangeAsync(agent, startOfToday, endOfToday);
            if (todayAttendance.Any())
            {
                presentCount++;
                var firstAttendance = todayAttendance.OrderBy(a => a.Timestamp).FirstOrDefault();
                if (firstAttendance != null)
                {
                    var agentName = $"{agent.User.FirstName} {agent.User.LastName}";
                    var time = firstAttendance.Timestamp?.ToString("HH:mm") ?? "--:--";
                    presentAgents.Add(new ManagerDashboardDTO.PresentAgent
                    {
                        Name = agentName,
                        Time = time
                    });
                }
            }
            else
            {
                absentCount++;
            }

            // Clients removed - no need to track client counts
            individualPerformance.Add(new ManagerDashboardDTO.IndividualPerformanceItem
            {
                Name = $"{agent.User.FirstName} {agent.User.LastName}",
                Clients = 0
            });
        }

        individualPerformance = individualPerformance.OrderByDescending(a => a.Clients).ToList();

        var groups = await groupService.GetGroupsByManagerAsync(manager);
        var groupPerformance = new List<ManagerDashboardDTO.GroupPerformanceItem>();

        foreach (var group in groups)
        {
            // Clients removed - no need to track group client counts
            groupPerformance.Add(new ManagerDashboardDTO.GroupPerformanceItem
            {
                Name = group.Name,
                Clients = 0
            });
        }

        groupPerformance = groupPerformance.OrderByDescending(a => a.Clients).ToList();

        // Set startTime and endTime from attendanceTimeframeService if available
        var startTime = "06:00";
        var endTime = "09:00";
        var timeframeEntity = await attendanceTimeframeService.GetTimeframeByManagerAsync(manager);
        if (timeframeEntity != null)
        {
            startTime = timeframeEntity.StartTime.ToString("HH:mm");
            endTime = timeframeEntity.EndTime.ToString("HH:mm");
        }

        var timeframe = new ManagerDashboardDTO.Timeframe
        {
            StartTime = startTime,
            EndTime = endTime
        };

        var rate = agents.Count() == 0 ? 0 : (int)Math.Round((presentCount * 100.0) / agents.Count());

        // For demo, recent activities are empty. You can fetch from auditLogService or similar.
        var recentActivities = new List<ManagerDashboardDTO.RecentActivity>();

        var dashboard = new ManagerDashboardDTO
        {
            Stats = new ManagerDashboardDTO.StatsModel()
            {
                TotalAgents = agents.Count(),
                ActiveToday = presentCount,
                ClientsCollected = 0,
                GroupsCount = groups.Count()
            },
            Attendance = new ManagerDashboardDTO.AttendanceModel()
            {
                Rate = rate,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                PresentAgents = presentAgents,
                Timeframe = timeframe
            },
            GroupPerformance = groupPerformance,
            IndividualPerformance = individualPerformance,
            RecentActivities = recentActivities
        };

        return Ok(dashboard);
    }

    /// <summary>
    /// Update attendance timeframe - Updates the allowed timeframe for agents to mark attendance
    /// </summary>
    [HttpPatch("dashboard/timeframe")]
    [ProducesResponseType(typeof(ManagerDashboardDTO.Timeframe), 200)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<ManagerDashboardDTO.Timeframe>> UpdateAttendanceTimeframe([FromBody] ManagerDashboardDTO.Timeframe timeframeRequest)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);

        // Parse time strings to TimeSpan
        if (!TimeSpan.TryParse(timeframeRequest.StartTime, out var startTime) ||
            !TimeSpan.TryParse(timeframeRequest.EndTime, out var endTime))
        {
            return BadRequest("Invalid time format");
        }

        var timeframe = await attendanceTimeframeService.SetTimeframeAsync(manager,TimeOnly.FromTimeSpan(startTime),TimeOnly.FromTimeSpan(endTime));

        return Ok(new ManagerDashboardDTO.Timeframe
        {
            StartTime = timeframe.StartTime.ToString(@"hh\:mm"),
            EndTime = timeframe.EndTime.ToString(@"hh\:mm")
        });
    }

    /// <summary>
    /// Get manager performance overview - Fetches a comprehensive performance overview, including KPIs, group performance, and individual agent performance.
    /// </summary>
    [HttpGet("performance")]
    public async Task<ActionResult<Dictionary<string, object>>> GetManagerPerformance([FromQuery] string period = "weekly")
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        
        var response = await managerService.GetPerformanceOverviewAsync(manager, period);
        return Ok(response);
    }

    // Clients endpoints removed

    /// <summary>
    /// Get attendance records for all agents (dashboard view) - Fetches attendance records and stats for all agents for the dashboard view.
    /// </summary>
    [HttpGet("attendance")]
    public async Task<ActionResult> GetAttendanceDashboard([FromQuery] string? date = null)
    {
        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
            var manager = await managerService.GetManagerByIdAsync(userId);
            
            var queryDate = !string.IsNullOrWhiteSpace(date) ? DateTime.Parse(date).Date : DateTime.Today;

            if (queryDate > DateTime.Today)
            {
                return BadRequest(new Dictionary<string, object>
                {
                    ["error"] = "Cannot query attendance for a future date",
                    ["date"] = queryDate.ToString("yyyy-MM-dd"),
                    ["records"] = new List<object>(),
                    ["stats"] = new Dictionary<string, object>(),
                    ["weeklySummary"] = new List<object>()
                });
            }

            var startOfDay = queryDate;
            var endOfDay = queryDate.AddDays(1).AddSeconds(-1);
            var agents = await agentService.GetAllAgentsAsync();

            var presentCount = 0;
            var lateCount = 0;
            var absentCount = 0;
            var allRecords = new List<Dictionary<string, object?>>();

            foreach (var agent in agents)
            {
                var agentCreated = agent.User.CreatedAt.Date;
                if (queryDate < agentCreated)
                {
                    continue;
                }

                var attendances = await agentService.GetAttendanceByAgentAndDateRangeAsync(agent, startOfDay, endOfDay);
                if (attendances.Any())
                {
                    var first = attendances.OrderBy(a => a.Timestamp).FirstOrDefault();
                    var isLate = false;
                    string? timeIn = null;

                    if (first != null)
                    {
                        timeIn = first.Timestamp?.ToString("HH:mm") ?? "--:--";
                        var status = await CalculateAttendanceStatusAsync(agent, first.Timestamp);
                        if (status == "late")
                        {
                            lateCount++;
                            isLate = true;
                        }
                        else
                        {
                            presentCount++;
                        }
                    }

                    var record = new Dictionary<string, object?>
                    {
                        ["id"] = first?.Id.ToString() ?? agent.UserId.ToString(),
                        ["agentName"] = $"{agent.User.FirstName} {agent.User.LastName}",
                        
                        ["date"] = queryDate.ToString("yyyy-MM-dd"),
                        ["timeIn"] = timeIn,
                        ["location"] = first?.Location ?? "",
                        ["status"] = isLate ? "late" : "present",
                        ["sector"] = first?.Sector
                    };
                    allRecords.Add(record);
                }
                else
                {
                    absentCount++;
                    var record = new Dictionary<string, object?>
                    {
                        ["id"] = agent.UserId.ToString(),
                        ["agentName"] = $"{agent.User.FirstName} {agent.User.LastName}",
                        
                        ["date"] = queryDate.ToString("yyyy-MM-dd"),
                        ["timeIn"] = null,
                        ["location"] = "",
                        ["status"] = "absent",
                        ["sector"] = null
                    };
                    allRecords.Add(record);
                }
            }

            var total = allRecords.Count;
            var attendanceRate = total == 0 ? 0 : (int)Math.Round(((presentCount + lateCount) * 100.0) / total);

            var startTime = "06:00";
            var endTime = "09:00";
            var timeframeEntity = await attendanceTimeframeService.GetLatestTimeframeAsync();
            if (timeframeEntity != null)
            {
                startTime = timeframeEntity.StartTime.ToString(@"hh\:mm");
                endTime = timeframeEntity.EndTime.ToString(@"hh\:mm");
            }

            var stats = new Dictionary<string, object>
            {
                ["attendanceRate"] = attendanceRate,
                ["presentCount"] = presentCount,
                ["lateCount"] = lateCount,
                ["absentCount"] = absentCount,
                ["timeframe"] = new Dictionary<string, object>
                {
                    ["startTime"] = startTime,
                    ["endTime"] = endTime
                }
            };

            var weeklySummary = new List<Dictionary<string, object>>();
            var monday = queryDate.AddDays(-(int)queryDate.DayOfWeek + 1);
            
            for (int i = 0; i < 7; i++)
            {
                var d = monday.AddDays(i);
                if (d > DateTime.Today) break;

                var s = d;
                var e = d.AddDays(1).AddSeconds(-1);
                var dayPresent = 0;
                var dayLate = 0;
                var dayAbsent = 0;
                var dayTotal = 0;

                foreach (var agent in agents)
                {
                    var agentCreated = agent.User.CreatedAt.Date;
                    if (d < agentCreated) continue;

                    var att = await agentService.GetAttendanceByAgentAndDateRangeAsync(agent, s, e);
                    if (att.Any())
                    {
                        var first = att.OrderBy(a => a.Timestamp).FirstOrDefault();
                        var status = await CalculateAttendanceStatusAsync(agent, first.Timestamp);
                        if (status == "late")
                        {
                            dayLate++;
                        }
                        else
                        {
                            dayPresent++;
                        }
                    }
                    else
                    {
                        dayAbsent++;
                    }
                    dayTotal++;
                }

                var dayRate = dayTotal == 0 ? 0 : (int)Math.Round(((dayPresent + dayLate) * 100.0) / dayTotal);
                var daySummary = new Dictionary<string, object>
                {
                    ["day"] = d.DayOfWeek.ToString().Substring(0, 1) + d.DayOfWeek.ToString().Substring(1).ToLower(),
                    ["date"] = d.ToString("yyyy-MM-dd"),
                    ["attendanceRate"] = dayRate
                };
                weeklySummary.Add(daySummary);
            }

            var response = new Dictionary<string, object>
            {
                ["date"] = queryDate.ToString("yyyy-MM-dd"),
                ["stats"] = stats,
                ["records"] = allRecords,
                ["weeklySummary"] = weeklySummary
            };

            if (!allRecords.Any())
            {
                return Ok(new Dictionary<string, object>
                {
                    ["date"] = queryDate.ToString("yyyy-MM-dd"),
                    ["message"] = "No agents existed on this date.",
                    ["records"] = new List<object>(),
                    ["stats"] = stats,
                    ["weeklySummary"] = weeklySummary
                });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new Dictionary<string, object>
            {
                ["error"] = "Failed to retrieve attendance data",
                ["message"] = ex.Message
            });
        }
    }

    /// <summary>
    /// Get attendance summary - Fetches a summary of attendance statistics.
    /// </summary>
    [HttpGet("attendance/summary")]
    public async Task<ActionResult<Dictionary<string, object>>> GetAttendanceSummary([FromQuery] string? date = null)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        
        var queryDate = !string.IsNullOrWhiteSpace(date) ? DateTime.Parse(date).Date : DateTime.Today;
        var startOfDay = queryDate;
        var endOfDay = queryDate.AddDays(1).AddSeconds(-1);
        var agents = await agentService.GetAllAgentsAsync();

        var present = 0;
        var late = 0;
        var absent = 0;

        foreach (var agent in agents)
        {
            var attendances = await agentService.GetAttendanceByAgentAndDateRangeAsync(agent, startOfDay, endOfDay);
            if (attendances.Any())
            {
                present++;
                var first = attendances.OrderBy(a => a.Timestamp).FirstOrDefault();
                var status = await CalculateAttendanceStatusAsync(agent, first.Timestamp);
                if (status == "late")
                {
                    late++;
                }
            }
            else
            {
                absent++;
            }
        }

        var total = agents.Count();
        var attendanceRate = total > 0 ? (int)((present / (double)total) * 100) : 0;

        var dailySummary = new Dictionary<string, object>
        {
            ["present"] = present,
            ["late"] = late,
            ["absent"] = absent,
            ["attendanceRate"] = attendanceRate
        };

        // Weekly trend
        var weeklyTrend = new List<Dictionary<string, object>>();
        var weekStart = queryDate.AddDays(-6);
        
        for (int i = 0; i < 7; i++)
        {
            var d = weekStart.AddDays(i);
            var s = d;
            var e = d.AddDays(1).AddSeconds(-1);
            var dayPresent = 0;

            foreach (var agent in agents)
            {
                var att = await agentService.GetAttendanceByAgentAndDateRangeAsync(agent, s, e);
                if (att.Any()) dayPresent++;
            }

            var dayRate = total > 0 ? (int)((dayPresent / (double)total) * 100) : 0;
            weeklyTrend.Add(new Dictionary<string, object>
            {
                ["day"] = d.DayOfWeek.ToString().Substring(0, 1).ToUpper() + d.DayOfWeek.ToString().Substring(1).ToLower(),
                ["rate"] = dayRate
            });
        }

        var response = new Dictionary<string, object>
        {
            ["dailySummary"] = dailySummary,
            ["weeklyTrend"] = weeklyTrend
        };

        return Ok(response);
    }

    /// <summary>
    /// Get my notification history - Retrieves paginated notification history for the authenticated manager, including stats.
    /// </summary>
    [HttpGet("notifications")]
    public async Task<ActionResult> GetManagerNotifications([FromQuery] int page = 1, [FromQuery] int limit = 10)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var currentUser = await userService.GetUserByIdAsync(userId);
        if (currentUser == null)
        {
            return BadRequest("User not found or account is inactive/deleted.");
        }
        var allNotifications = await notificationService.GetNotificationsByRecipientAsync(currentUser);

        var total = allNotifications.Count();
        var fromIndex = Math.Min((page - 1) * limit, total);
        var toIndex = Math.Min(fromIndex + limit, total);
        var paged = allNotifications.Skip(fromIndex).Take(toIndex - fromIndex).ToList();

        var notifications = paged.Select(n => new Dictionary<string, object>
        {
            ["id"] = n.Id.ToString(),
            ["title"] = n.Title,
            ["message"] = n.Message.Contains(": ") ? n.Message.Split(": ", 2)[1] : n.Message,
            ["recipient"] = n.Recipient?.Email ?? "All Users",
            ["status"] = n.Status,
            ["sentAt"] = n.SentAt.HasValue ? n.SentAt.Value.ToString("o") : DateTime.UtcNow.ToString("o"),
            ["read"] = n.ReadStatus,
            ["priority"] = n.Priority.ToString().ToLower(),
            ["sender"] = n.Sender != null ? new Dictionary<string, object>
            {
                ["role"] = n.Sender.Role.ToString().ToLower(),
                ["name"] = $"{n.Sender.FirstName} {n.Sender.LastName}"
            } : throw new InvalidOperationException("null value for sender in managercontroller")
        }).ToList();

        var stats = new Dictionary<string, object>
        {
            ["totalSent"] = total,
            ["thisWeek"] = allNotifications.Count(n => n.SentAt != null && n.SentAt > DateTime.UtcNow.AddDays(-7)),
            ["readRate"] = 0.0 // Placeholder
        };

        var pagination = new Dictionary<string, object>
        {
            ["page"] = page,
            ["limit"] = limit,
            ["total"] = total
        };

        var response = new Dictionary<string, object>
        {
            ["notifications"] = notifications,
            ["stats"] = stats,
            ["pagination"] = pagination
        };

        return Ok(response);
    }

    /// <summary>
    /// Send a notification (manager) - Sends a notification to all agents, a group, or a specific user.
    /// </summary>
    [HttpPost("notifications")]
    [ProducesResponseType(201)]
    public async Task<ActionResult> SendManagerNotification([FromBody] NotificationRequestDTO body)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var sender = await userService.GetUserByIdAsync(userId);
        if (sender == null)
        {
            return BadRequest("User not found or account is inactive/deleted.");
        }

        if (!string.IsNullOrEmpty(body.SenderRole) && !body.SenderRole.Equals(sender.Role.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { error = "Sender role mismatch" });
        }

        // WorkId validation removed

        var recipients = new List<User>();
        var resolvedRecipient = body.Recipient;

        // Recipient resolution
        if (body.Recipient.Equals("All Users", StringComparison.OrdinalIgnoreCase))
        {
            recipients = (await userService.GetActiveUsersAsync()).ToList();
        }
        else if (body.Recipient.Equals("All Agents", StringComparison.OrdinalIgnoreCase))
        {
            recipients = (await userService.GetUsersByRoleAsync(Role.AGENT)).Select(dto => new User 
            { 
                Id = long.Parse(dto.Id.ToString()), 
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role
            }).ToList();
        }
        else if (body.Recipient.Equals("All Managers", StringComparison.OrdinalIgnoreCase))
        {
            recipients = (await userService.GetUsersByRoleAsync(Role.MANAGER)).Select(dto => new User 
            { 
                Id = long.Parse(dto.Id.ToString()), 
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role
            }).ToList();
        }
        else if (body.Recipient.Contains("@"))
        {
            var user = await userService.GetUserByEmailAsync(body.Recipient);
            if (user != null) recipients.Add(user);
        }
        else
        {
            var groups = await groupService.GetAllGroupsAsync();
            var group = groups.FirstOrDefault(g => g.Name.Equals(body.Recipient, StringComparison.OrdinalIgnoreCase));
            if (group != null)
            {
                recipients.AddRange(group.Agents.Select(a => a.User));
            }
            else if (long.TryParse(body.Recipient, out var idTarget))
            {
                var user = await userService.GetUserByIdAsync(idTarget);
                if (user != null) recipients.Add(user);
            }
        }

        // Notification sending
        Notification? lastNotification = null;
        var category = Category.SYSTEM;
        var priorityEnum = Enum.TryParse<Priority>(body.Priority, true, out var parsedPriority)
            ? parsedPriority
            : Priority.MEDIUM;

        if (body.Recipient.Equals("All Users", StringComparison.OrdinalIgnoreCase))
        {
            lastNotification = await notificationService.SendCompleteNotificationToAllAsync(
                sender, body.Title, $"{body.Title}: {body.Message}", false, category, priorityEnum
            );
        }
        else
        {
            foreach (var user in recipients)
            {
                lastNotification = await notificationService.SendCompleteNotificationAsync(
                    sender, user, body.Title, $"{body.Title}: {body.Message}", false, category, priorityEnum
                );
            }
        }

        // Build response
        var response = new Dictionary<string, object>
        {
            ["id"] = lastNotification?.Id.ToString() ?? "0",
            ["title"] = body.Title,
            ["message"] = body.Message,
            ["recipient"] = resolvedRecipient,
            ["priority"] = body.Priority,
            ["status"] = "sent",
            ["sentAt"] = DateTime.UtcNow.ToString("o"),
            ["readBy"] = 0,
            ["totalRecipients"] = recipients.Count(), // This is fine - it's counting the actual recipients
            ["sender"] = new Dictionary<string, object>
            {
                ["role"] = sender.Role.ToString().ToLower(),
                ["workId"] = sender.WorkId ?? "",
                ["name"] = $"{sender.FirstName} {sender.LastName}"
            }
        };

        return StatusCode(201, response);
    }

    /// <summary>
    /// Calculate attendance status based on timeframe and 5-minute rule
    /// </summary>
    private async Task<string> CalculateAttendanceStatusAsync(Agent agent, DateTime? timestamp)
    {
        if (!timestamp.HasValue)
            return "present";
            
        // Get the attendance timeframe for the manager
        var timeframe = await attendanceTimeframeService.GetTimeframeByManagerAsync(agent.Manager);
        
        var startTime = timeframe?.StartTime ?? new TimeOnly(6, 0); // Default 6:00 AM
        var endTime = timeframe?.EndTime ?? new TimeOnly(9, 0); // Default 9:00 AM
        
        var attendanceTime = TimeOnly.FromDateTime(timestamp.Value);
        
        // Check if attendance is within the timeframe
        if (attendanceTime < startTime || attendanceTime > endTime)
        {
            return "late"; // Outside timeframe is always late
        }
        
        // Check if there are 5 minutes or less remaining in the timeframe
        var timeRemaining = endTime - attendanceTime;
        if (timeRemaining.TotalMinutes <= 5)
        {
            return "late"; // 5 minutes or less remaining = late
        }
        
        return "present"; // Within timeframe with more than 5 minutes remaining
    }

    /// <summary>
    /// Helper method to map Group to GroupDetailDTO
    /// </summary>
    private GroupDetailDTO MapGroupToDetailDtoAsync(Group group)
    {
        var groupId = $"group-{group.Id:000}";
        var createdAt = group.CreatedAt.ToString("yyyy-MM-dd");
        var agents = new List<GroupDetailDTO.AgentDTO>();
        var collectedClients = 0;

        foreach (var agent in group.Agents)
        {
            agents.Add(new GroupDetailDTO.AgentDTO
            {
                Id = $"agent-{agent.UserId:000}",
                Name = $"{agent.User.FirstName} {agent.User.LastName}"
            });
        }

        GroupDetailDTO.AgentDTO? teamLeader = null;
        if (group.Leader != null)
        {
            teamLeader = new GroupDetailDTO.AgentDTO
            {
                Id = $"agent-{group.Leader.UserId:000}",
                Name = $"{group.Leader.User.FirstName} {group.Leader.User.LastName}"
            };
        }

        var performance = 0;

        return new GroupDetailDTO
        {
            Id = groupId,
            Name = group.Name,
            Description = group.Description ?? "",
            Agents = agents,
            TeamLeader = teamLeader,
            Performance = performance,
            CollectedClients = collectedClients,
            CreatedAt = createdAt
        };
    }


}