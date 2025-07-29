using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace backend.Services;

public class AgentService : IAgentService
{
    private readonly ILogger<AgentService> _logger;
    private readonly IAgentRepository _agentRepository;
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAttendanceService _attendanceService;
    private readonly IClientsCollectedService _clientsCollectedService;
    private readonly ApplicationDbContext _context;

    public AgentService(
        ILogger<AgentService> logger,
        IAgentRepository agentRepository,
        IUserService userService,
        IAuditLogService auditLogService,
        IAttendanceService attendanceService,
        IClientsCollectedService clientsCollectedService,
        ApplicationDbContext context)
    {
        _logger = logger;
        _agentRepository = agentRepository;
        _userService = userService;
        _auditLogService = auditLogService;
        _attendanceService = attendanceService;
        _clientsCollectedService = clientsCollectedService;
        _context = context;
    }

    public async Task<Agent> CreateAgentAsync(string firstName, string lastName, string phoneNumber, string nationalId,
                                            string email, string workId, string? password, Manager manager, 
                                            Agent.AgentType agentType, string sector)
    {
        try
        {
            _logger.LogInformation("Creating agent: firstName={FirstName}, lastName={LastName}, email={Email}, workId={WorkId}, agentType={AgentType}, sector={Sector}",
                firstName, lastName, email, workId, agentType, sector);
                
            var managerUser = manager.User;
            
            // Create user with AGENT role
            var agentUser = await _userService.CreateUserAsync(firstName, lastName, phoneNumber, nationalId,
                                                              email, workId, password, Role.Agent, managerUser);
            
            // Ensure user is persisted and has an ID
            if (agentUser.Id == 0)
            {
                var errorMsg = "Failed to create agent: User ID is null";
                _logger.LogError(errorMsg);
                throw new InvalidOperationException(errorMsg);
            }
            
            _logger.LogInformation("Creating agent with user ID: {UserId}", agentUser.Id);
            
            // Create a completely new Agent instance
            var agent = new Agent
            {
                UserId = agentUser.Id,
                User = agentUser,
                Manager = manager,
                AgentType = agentType != Agent.AgentType.Individual ? agentType : Agent.AgentType.Individual,
                Sector = !string.IsNullOrWhiteSpace(sector) ? sector : "General"
            };
            
            // Use raw SQL insert to bypass any ORM issues
            var insertSql = "INSERT INTO agents (user_id, manager_id, agent_type, sector) VALUES (@UserId, @ManagerId, @AgentType, @Sector)";
            await _context.Database.ExecuteSqlRawAsync(insertSql, 
                new Microsoft.Data.SqlClient.SqlParameter("@UserId", agent.UserId),
                new Microsoft.Data.SqlClient.SqlParameter("@ManagerId", manager.UserId),
                new Microsoft.Data.SqlClient.SqlParameter("@AgentType", agent.AgentType.ToString()),
                new Microsoft.Data.SqlClient.SqlParameter("@Sector", agent.Sector));
                
            // Refresh the entity to reflect the inserted state
            await _context.SaveChangesAsync();
            
            // Find the agent directly using repository
            var savedAgent = await _agentRepository.GetByIdAsync(agentUser.Id);
            if (savedAgent == null)
            {
                throw new InvalidOperationException("Failed to retrieve agent after creation");
            }
            
            _logger.LogInformation("Agent created successfully with ID: {AgentId}", savedAgent.UserId);
            
            // Log the action
            await _auditLogService.LogEventAsync(
                    "CREATE_AGENT",
                    "AGENT",
                    savedAgent.UserId.ToString(),
                    managerUser,
                    $"Agent created: {firstName} {lastName} ({email}), Type: {agentType}, Sector: {sector}"
            );
            
            return savedAgent;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating agent: {Message}", e.Message);
            throw new InvalidOperationException($"Failed to create agent: {e.Message}", e);
        }
    }

    public async Task<Agent> GetAgentByIdAsync(long id)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent not found with ID: {id}");
        }
        return agent;
    }

    public async Task<IEnumerable<Agent>> GetAllAgentsAsync()
    {
        return await _agentRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Agent>> GetAgentsByManagerAsync(Manager manager)
    {
        return await _agentRepository.GetByManagerAsync(manager);
    }

    public async Task<IEnumerable<Agent>> GetAgentsByTypeAsync(Agent.AgentType agentType)
    {
        var allAgents = await _agentRepository.GetAllAsync();
        return allAgents.Where(agent => agent.AgentType == agentType);
    }

    public async Task<IEnumerable<Agent>> GetAgentsByManagerAndTypeAsync(Manager manager, Agent.AgentType agentType)
    {
        return await _agentRepository.GetByManagerAndAgentTypeAsync(manager, agentType);
    }

    public async Task<Agent> UpdateAgentSectorAsync(long id, string sector)
    {
        var agent = await GetAgentByIdAsync(id);
        agent.Sector = sector;
        
        var updatedAgent = await _agentRepository.UpdateAsync(agent);
        
        await _auditLogService.LogEventAsync(
                "UPDATE_AGENT",
                "AGENT",
                id.ToString(),
                agent.Manager.User,
                $"Agent sector updated: {sector}"
        );
        
        return updatedAgent;
    }

    public async Task<Agent> UpdateAgentTypeAsync(long id, Agent.AgentType agentType)
    {
        var agent = await GetAgentByIdAsync(id);
        agent.AgentType = agentType;
        
        var updatedAgent = await _agentRepository.UpdateAsync(agent);
        
        await _auditLogService.LogEventAsync(
                "UPDATE_AGENT",
                "AGENT",
                id.ToString(),
                agent.Manager.User,
                $"Agent type updated: {agentType}"
        );
        
        return updatedAgent;
    }

    public async Task DeactivateAgentAsync(long id)
    {
        var agent = await GetAgentByIdAsync(id);
        
        // Deactivate user account
        await _userService.UpdateUserStatusAsync(agent.UserId, false);
        
        await _auditLogService.LogEventAsync(
                "DEACTIVATE_AGENT",
                "AGENT",
                id.ToString(),
                agent.Manager.User,
                "Agent deactivated"
        );
    }

    public async Task<Agent> UpdateAgentAsync(long id, Dictionary<string, object> updateFields)
    {
        var agent = await GetAgentByIdAsync(id);
        
        // Update fields based on the dictionary
        if (updateFields.ContainsKey("sector"))
        {
            agent.Sector = (string)updateFields["sector"];
        }
        
        if (updateFields.ContainsKey("agentType"))
        {
            var agentTypeStr = (string)updateFields["agentType"];
            agent.AgentType = Enum.Parse<Agent.AgentType>(agentTypeStr);
        }
        
        // Handle group update (can be null)
        if (updateFields.ContainsKey("group"))
        {
            var groupObj = updateFields["group"];
            if (groupObj == null)
            {
                agent.Group = null;
            }
            else if (groupObj is Group group)
            {
                agent.Group = group;
            }
        }
        
        // Update user fields if provided
        var userFields = new Dictionary<string, object>();
        if (updateFields.ContainsKey("firstName") || 
            updateFields.ContainsKey("lastName") || 
            updateFields.ContainsKey("phoneNumber") || 
            updateFields.ContainsKey("email"))
        {
            if (updateFields.ContainsKey("firstName"))
            {
                userFields["firstName"] = updateFields["firstName"];
            }
            if (updateFields.ContainsKey("lastName"))
            {
                userFields["lastName"] = updateFields["lastName"];
            }
            if (updateFields.ContainsKey("phoneNumber"))
            {
                userFields["phoneNumber"] = updateFields["phoneNumber"];
            }
            if (updateFields.ContainsKey("email"))
            {
                userFields["email"] = updateFields["email"];
            }
            
            // Update user info if any user fields were provided
            if (userFields.Any())
            {
                var user = agent.User;
                if (userFields.ContainsKey("firstName"))
                {
                    user.FirstName = (string)userFields["firstName"];
                }
                if (userFields.ContainsKey("lastName"))
                {
                    user.LastName = (string)userFields["lastName"];
                }
                if (userFields.ContainsKey("phoneNumber"))
                {
                    user.PhoneNumber = (string)userFields["phoneNumber"];
                }
                if (userFields.ContainsKey("email"))
                {
                    user.Email = (string)userFields["email"];
                }
            }
        }
        
        var updatedAgent = await _agentRepository.UpdateAsync(agent);
        
        await _auditLogService.LogEventAsync(
                "UPDATE_AGENT",
                "AGENT",
                id.ToString(),
                agent.Manager.User,
                $"Agent updated: {string.Join(", ", updateFields.Keys)}"
        );
        
        return updatedAgent;
    }

    public async Task DeleteAgentAsync(long id)
    {
        var agent = await GetAgentByIdAsync(id);
        
        // Log the action before deletion
        await _auditLogService.LogEventAsync(
                "DELETE_AGENT",
                "AGENT",
                id.ToString(),
                agent.Manager.User,
                $"Agent deleted: {agent.User.FirstName} {agent.User.LastName}"
        );
        
        // Delete the agent
        await _agentRepository.DeleteAsync(id);
        
        // Deactivate the associated user instead of deleting
        await _userService.UpdateUserStatusAsync(agent.UserId, false);
    }

    public async Task<Dictionary<string, object>> GetGroupPerformanceAsync(Group group, DateTime startDateTime, DateTime endDateTime)
    {
        // Get all agents in the group
        var groupAgents = group.Agents.ToList();
        
        // Calculate aggregate metrics
        var totalAttendance = 0;
        var totalClientsCollected = 0L;
        
        foreach (var agent in groupAgents)
        {
            // Get attendance for each agent
            var agentAttendances = await _attendanceService.GetAttendanceByAgentAndDateRangeAsync(agent, startDateTime, endDateTime);
            totalAttendance += agentAttendances.Count();
            
            // Get clients collected for each agent
            var agentClients = await _clientsCollectedService.CountClientsByAgentAndDateRangeAsync(agent, startDateTime, endDateTime);
            totalClientsCollected += agentClients;
        }
        
        // Calculate averages
        var agentCount = groupAgents.Count;
        var avgAttendance = agentCount > 0 ? (double)totalAttendance / agentCount : 0;
        var avgClientsCollected = agentCount > 0 ? (double)totalClientsCollected / agentCount : 0;
        
        // Create result dictionary
        var result = new Dictionary<string, object>
        {
            ["groupId"] = group.Id,
            ["groupName"] = group.Name,
            ["memberCount"] = agentCount,
            ["totalAttendance"] = totalAttendance,
            ["averageAttendance"] = avgAttendance,
            ["totalClientsCollected"] = totalClientsCollected,
            ["averageClientsCollected"] = avgClientsCollected,
            ["startDate"] = startDateTime.Date.ToString("yyyy-MM-dd"),
            ["endDate"] = endDateTime.Date.ToString("yyyy-MM-dd")
        };
        
        // Include team leader info if available
        if (group.Leader != null)
        {
            var leaderUser = group.Leader.User;
            result["teamLeader"] = new Dictionary<string, object>
            {
                ["id"] = group.Leader.UserId,
                ["email"] = leaderUser.Email,
                ["workId"] = leaderUser.WorkId
            };
        }
        
        return result;
    }
    
    public async Task<IEnumerable<Attendance>> GetAttendanceByAgentAndDateRangeAsync(Agent agent, DateTime startDateTime, DateTime endDateTime)
    {
        // Delegate to the AttendanceService
        return await _attendanceService.GetAttendanceByAgentAndDateRangeAsync(agent, startDateTime, endDateTime);
    }
    
    public async Task<long> CountClientsByAgentAndDateRangeAsync(Agent agent, DateTime startDateTime, DateTime endDateTime)
    {
        // Delegate to the ClientsCollectedService
        return await _clientsCollectedService.CountClientsByAgentAndDateRangeAsync(agent, startDateTime, endDateTime);
    }
}