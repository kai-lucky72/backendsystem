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
[Authorize(Roles = "Admin,Manager")]
public class ManagerController(
    IManagerService managerService,
    IAgentService agentService,
    IGroupService groupService,
    IAttendanceTimeframeService attendanceTimeframeService,
    IUserService userService,
    IClientService clientService,
    INotificationService notificationService)
    : ControllerBase
{
    /// <summary>
    /// Create agent - Creates a new agent under the manager's supervision according to frontend requirements. The 'type' field accepts 'individual' or 'sales' values.
    /// </summary>
    [HttpPost("agents")]
    [ProducesResponseType(typeof(UserDTO), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 409)]
    [ProducesResponseType(typeof(ApiErrorResponse), 500)]
    public async Task<ActionResult> CreateAgent([FromBody] CreateAgentRequest request)
    {
        try
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
            var manager = await managerService.GetManagerByIdAsync(userId);

            // Validate workId format to avoid common issues
            if (!string.IsNullOrEmpty(request.WorkId) && !System.Text.RegularExpressions.Regex.IsMatch(request.WorkId, @"^[A-Z0-9]{5,10}$"))
            {
                return BadRequest(new ApiErrorResponse
                {
                    Status = 400,
                    Message = "Invalid workId format",
                    Timestamp = DateTime.UtcNow,
                    Details = "workId must be 5-10 uppercase letters and numbers"
                });
            }

            // Check if email is unique
            if (await userService.IsEmailTakenAsync(request.Email))
            {
                return Conflict(new ApiErrorResponse
                {
                    Status = 409,
                    Message = "Email already in use",
                    Timestamp = DateTime.UtcNow,
                    Details = "Another user is already registered with this email"
                });
            }

            // Check if workId is unique
            if (await userService.IsWorkIdTakenAsync(request.WorkId))
            {
                return Conflict(new ApiErrorResponse
                {
                    Status = 409,
                    Message = "Work ID already in use",
                    Timestamp = DateTime.UtcNow,
                    Details = "Another user is already registered with this work ID"
                });
            }

            // Check if the phone number is unique
            if (await userService.IsPhoneNumberTakenAsync(request.PhoneNumber))
            {
                return Conflict(new ApiErrorResponse
                {
                    Status = 409,
                    Message = "Phone number already in use",
                    Timestamp = DateTime.UtcNow,
                    Details = "Another user is already registered with this phone number"
                });
            }

            // Use the default password if not provided
            var password = string.IsNullOrWhiteSpace(request.Password) ? "Temp@1234" : request.Password;

            var agent = await agentService.CreateAgentAsync(
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

            // Format ID and determine if agent is a team leader
            var formattedId = $"agt-{agent.UserId:000}";
            var isTeamLeader = agent.Group?.Leader?.UserId == agent.UserId;
            var groupName = agent.Group?.Name ?? "";

            var agentDto = new UserDTO
            {
                Id = formattedId,
                FirstName = agent.User.FirstName,
                LastName = agent.User.LastName,
                PhoneNumber = agent.User.PhoneNumber ?? throw new InvalidOperationException(),
                NationalId = agent.User.NationalId ?? throw new InvalidOperationException(),
                Email = agent.User.Email,
                WorkId = agent.User.WorkId,
                Role = agent.User.Role,
                CreatedAt = agent.User.CreatedAt.ToString("yyyy-MM-dd"),
                Active = agent.User.Active,
                Type = agent.AgentType.ToString().ToLower(),
                Sector = agent.Sector,
                Group = groupName,
                IsTeamLeader = isTeamLeader,
                Status = agent.User.Active ? "active" : "inactive",
                ClientsCollected = 0,
                AttendanceRate = 0
            };

            return Ok(agentDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiErrorResponse
            {
                Status = 500,
                Message = "Failed to create agent",
                Timestamp = DateTime.UtcNow,
                Details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get all agents - Retrieves all agents under the manager's supervision with their detailed information
    /// </summary>
    [HttpGet("agents")]
    [ProducesResponseType(typeof(List<AgentListItemDTO>), 200)]
    public async Task<ActionResult<List<AgentListItemDTO>>> GetAgents()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        var agents = await agentService.GetAgentsByManagerAsync(manager);
        
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var today = DateTime.UtcNow;

        var agentDto = new List<AgentListItemDTO>();
        
        foreach (var agent in agents)
        {
            var formattedId = $"agt-{agent.UserId:000}";
            var groupName = agent.Group?.Name ?? "";
            var isTeamLeader = agent.Group?.Leader?.UserId == agent.UserId;
            var status = agent.User.Active ? "active" : "inactive";
            
            int clientsCollected = (int)await agentService.CountClientsByAgentAndDateRangeAsync(agent, thirtyDaysAgo, today);
            var attendances = await agentService.GetAttendanceByAgentAndDateRangeAsync(agent, thirtyDaysAgo, today);
            var attendanceRate = (int)Math.Round((attendances.Count() / 30.0) * 100);

            agentDto.Add(new AgentListItemDTO
            {
                Id = formattedId,
                FirstName = agent.User.FirstName,
                LastName = agent.User.LastName,
                Email = agent.User.Email,
                PhoneNumber = agent.User.PhoneNumber ?? throw new InvalidOperationException(),
                NationalId = agent.User.NationalId ?? throw new InvalidOperationException(),
                WorkId = agent.User.WorkId,
                Type = agent.AgentType.ToString().ToLower(),
                Group = groupName,
                IsTeamLeader = isTeamLeader,
                Status = status,
                ClientsCollected = clientsCollected,
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
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);

        // Check if an agent belongs to this manager
        var agent = await agentService.GetAgentByIdAsync(id);
        if (agent.Manager.UserId != manager.UserId)
        {
            return Forbid();
        }

        // Update agent with the provided fields
        var updatedAgent = await agentService.UpdateAgentAsync(id, updateRequest);

        // Get additional data for the response
        var formattedId = $"agt-{updatedAgent.UserId:000}";
        var isTeamLeader = updatedAgent.Group?.Leader?.UserId == updatedAgent.UserId;
        var groupName = updatedAgent.Group?.Name ?? "";

        // Calculate metrics
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var today = DateTime.UtcNow;

        var clientsCollected = await agentService.CountClientsByAgentAndDateRangeAsync(updatedAgent, thirtyDaysAgo, today);
        var attendances = await agentService.GetAttendanceByAgentAndDateRangeAsync(updatedAgent, thirtyDaysAgo, today);
        var attendanceRate = (int)Math.Round((attendances.Count() / 30.0) * 100);

        var updatedAgentDto = new UserDTO
        {
            Id = formattedId,
            FirstName = updatedAgent.User.FirstName,
            LastName = updatedAgent.User.LastName,
            Email = updatedAgent.User.Email,
            PhoneNumber = updatedAgent.User.PhoneNumber ?? throw new InvalidOperationException(),
            NationalId = updatedAgent.User.NationalId ?? throw new InvalidOperationException(),
            WorkId = updatedAgent.User.WorkId,
            Role = updatedAgent.User.Role,
            CreatedAt = updatedAgent.User.CreatedAt.ToString("yyyy-MM-dd") ?? "",
            Active = updatedAgent.User.Active,
            Type = updatedAgent.AgentType.ToString().ToLower(),
            Sector = updatedAgent.Sector,
            Group = groupName,
            IsTeamLeader = isTeamLeader,
            Status = updatedAgent.User.Active ? "active" : "inactive",
            ClientsCollected = (int)clientsCollected,
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
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);

        // Check if an agent belongs to this manager
        var agent = await agentService.GetAgentByIdAsync(id);
        if (agent.Manager.UserId != manager.UserId)
        {
            return Forbid();
        }

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
                ["workId"] = group.Leader.User.WorkId
            } : null,
            ["agents"] = group.Agents.Select(agent => new Dictionary<string, object?>
            {
                ["id"] = $"agt-{agent.UserId:000}",
                ["name"] = $"{agent.User.FirstName} {agent.User.LastName}",
                ["workId"] = agent.User.WorkId
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
        return StatusCode(201, await MapGroupToDetailDtoAsync(group));
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
        return Ok(await MapGroupToDetailDtoAsync(updated));
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
                errors.Add($"Agent {agent.User.WorkId} is not a SALES agent");
                continue;
            }

            if (agent.Group != null && agent.Group.Id != groupId)
            {
                errors.Add($"Agent {agent.User.WorkId} is already assigned to another group");
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
                ["lastName"] = updated.Leader.User.LastName,
                ["workId"] = updated.Leader.User.WorkId
            },
            ["agents"] = updated.Agents.Select(agent => new Dictionary<string, object?>
            {
                ["id"] = agent.UserId,
                ["firstName"] = agent.User.FirstName,
                ["lastName"] = agent.User.LastName,
                ["workId"] = agent.User.WorkId,
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
                ["workId"] = updatedGroup.Leader.User.WorkId
            },
            ["agents"] = updatedGroup.Agents.Select(a => new Dictionary<string, object?>
            {
                ["id"] = a.UserId,
                ["firstName"] = a.User.FirstName,
                ["lastName"] = a.User.LastName,
                ["workId"] = a.User.WorkId,
                ["email"] = a.User.Email,
                ["phoneNumber"] = a.User.PhoneNumber ?? throw new InvalidOperationException(),
                ["nationalId"] = a.User.NationalId ?? throw new InvalidOperationException(),
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
                    WorkId = agent.User.WorkId,
                    Role = agent.User.Role,
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
                    ["lastName"] = updatedGroup.Leader.User.LastName,
                    ["workId"] = updatedGroup.Leader.User.WorkId
                },
                ["agents"] = updatedGroup.Agents.Select(agent => new Dictionary<string, object?>
                {
                    ["id"] = agent.UserId,
                    ["firstName"] = agent.User.FirstName,
                    ["lastName"] = agent.User.LastName,
                    ["workId"] = agent.User.WorkId,
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
                ["lastName"] = group.Leader.User.LastName,
                ["workId"] = group.Leader.User.WorkId
            },
            ["agents"] = group.Agents.Select(agent => new Dictionary<string, object?>
            {
                ["id"] = agent.UserId,
                ["firstName"] = agent.User.FirstName,
                ["lastName"] = agent.User.LastName,
                ["workId"] = agent.User.WorkId,
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

            // Get client count
            var clientCount = await agentService.CountClientsByAgentAndDateRangeAsync(agent, startDateTime, endDateTime);

            return Ok(new Dictionary<string, object>
            {
                ["agent"] = new UserDTO
                {
                    Id = $"agt-{agent.UserId:000}",
                    Email = agent.User.Email,
                    WorkId = agent.User.WorkId,
                    FirstName = agent.User.FirstName,
                    LastName = agent.User.LastName,
                    Role = agent.User.Role,
                    Type = agent.AgentType.ToString().ToLower(),
                    Status = agent.User.Active ? "active" : "inactive"
                },
                ["attendanceCount"] = attendances.Count(),
                ["clientsCollected"] = clientCount,
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
        var agents = await agentService.GetAgentsByManagerAsync(manager);
        
        var startOfToday = DateTime.Today;
        var endOfToday = DateTime.Today.AddDays(1).AddSeconds(-1);

        var presentCount = 0;
        var absentCount = 0;
        var presentAgents = new List<ManagerDashboardDTO.PresentAgent>();
        var totalClients = 0;
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

            var agentClients = await agentService.CountClientsByAgentAndDateRangeAsync(agent, startOfToday, endOfToday);
            totalClients += (int)agentClients;
            individualPerformance.Add(new ManagerDashboardDTO.IndividualPerformanceItem
            {
                Name = $"{agent.User.FirstName} {agent.User.LastName}",
                Clients = (int)agentClients
            });
        }

        individualPerformance = individualPerformance.OrderByDescending(a => a.Clients).ToList();

        var groups = await groupService.GetGroupsByManagerAsync(manager);
        var groupPerformance = new List<ManagerDashboardDTO.GroupPerformanceItem>();

        foreach (var group in groups)
        {
            var groupClients = 0;
            foreach (var member in group.Agents)
            {
                groupClients += (int)await agentService.CountClientsByAgentAndDateRangeAsync(member, startOfToday, endOfToday);
            }
            groupPerformance.Add(new ManagerDashboardDTO.GroupPerformanceItem
            {
                Name = group.Name,
                Clients = groupClients
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
                ClientsCollected = totalClients,
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

    /// <summary>
    /// Get all clients collected by manager's agents (paginated) - Fetches a paginated list of all clients collected by the manager's agents.
    /// </summary>
    [HttpGet("performance/clients")]
    public async Task<ActionResult<Dictionary<string, object>>> GetManagerClients(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException());
        var manager = await managerService.GetManagerByIdAsync(userId);
        
        var clientPage = await managerService.GetClientsCollectedAsync(manager, search, page - 1, limit);
        
        var response = new Dictionary<string, object>
        {
            ["clients"] = clientPage.Clients,
            ["pagination"] = new Dictionary<string, object>
            {
                ["page"] = page,
                ["limit"] = limit,
                ["total"] = clientPage.TotalCount
            }
        };

        return Ok(response);
    }

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
            var agents = await agentService.GetAgentsByManagerAsync(manager);

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
                        if (first.Timestamp?.TimeOfDay > new TimeSpan(8, 0, 0))
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
                        ["workId"] = agent.User.WorkId,
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
                        ["workId"] = agent.User.WorkId,
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
            var timeframeEntity = await attendanceTimeframeService.GetTimeframeByManagerAsync(manager);
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
                        if (first != null && first.Timestamp?.TimeOfDay > new TimeSpan(8, 0, 0))
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
        var agents = await agentService.GetAgentsByManagerAsync(manager);

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
                if (first != null && first.Timestamp?.TimeOfDay > new TimeSpan(8, 0, 0))
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
            ["readBy"] = n.ReadBy,
            ["totalRecipients"] = n.TotalRecipients,
            ["priority"] = n.Priority.ToString().ToLower(),
            ["sender"] = n.Sender != null ? new Dictionary<string, object>
            {
                ["role"] = n.Sender.Role.ToString().ToLower(),
                ["workId"] = n.Sender.WorkId ?? throw new InvalidOperationException("null value for workid in managercontroller"),
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

        if (!string.IsNullOrEmpty(body.SenderRole) && !body.SenderRole.Equals(sender.Role.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { error = "Sender role mismatch" });
        }

        if (!string.IsNullOrEmpty(body.SenderWorkId) && !body.SenderWorkId.Equals(sender.WorkId))
        {
            return StatusCode(403, new { error = "Sender workId mismatch" });
        }

        var recipients = new List<User>();
        var resolvedRecipient = body.Recipient;

        // Recipient resolution
        if (body.Recipient.Equals("All Users", StringComparison.OrdinalIgnoreCase))
        {
            recipients = (await userService.GetAllUsersAsync()).ToList();
        }
        else if (body.Recipient.Equals("All Agents", StringComparison.OrdinalIgnoreCase))
        {
            recipients = (await userService.GetUsersByRoleAsync(Role.AGENT)).Select(dto => new User 
            { 
                Id = long.Parse(dto.Id.ToString()), 
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role,
                WorkId = dto.WorkId
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
                Role = dto.Role,
                WorkId = dto.WorkId
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
            ["totalRecipients"] = recipients.Count(),
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
    /// Helper method to map Group to GroupDetailDTO
    /// </summary>
    private async Task<GroupDetailDTO> MapGroupToDetailDtoAsync(Group group)
    {
        var groupId = $"group-{group.Id:000}";
        var createdAt = group.CreatedAt.ToString("yyyy-MM-dd");
        var agents = new List<GroupDetailDTO.AgentDTO>();
        var collectedClients = 0;

        foreach (var agent in group.Agents)
        {
            var agentClients = await clientService.CountClientsByAgentAsync(agent);
            agent.ClientsCollected = (int)agentClients;
            collectedClients += (int)agentClients;
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

        var performance = group.Agents.Count > 0 ? (int)((collectedClients / (double)group.Agents.Count) * 100) : 0;

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