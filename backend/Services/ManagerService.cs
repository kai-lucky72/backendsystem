using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class ManagerService : IManagerService
{
    private readonly ILogger<ManagerService> _logger;
    private readonly IManagerRepository _managerRepository;
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;
    private readonly IAgentService _agentService;
    private readonly IGroupService _groupService;
    private readonly IClientService _clientService;
    private readonly IAttendanceService _attendanceService;
    private readonly ApplicationDbContext _context;

    public ManagerService(
        ILogger<ManagerService> logger,
        IManagerRepository managerRepository,
        IUserService userService,
        IAuditLogService auditLogService,
        IAgentService agentService,
        IGroupService groupService,
        IClientService clientService,
        IAttendanceService attendanceService,
        ApplicationDbContext context)
    {
        _logger = logger;
        _managerRepository = managerRepository;
        _userService = userService;
        _auditLogService = auditLogService;
        _agentService = agentService;
        _groupService = groupService;
        _clientService = clientService;
        _attendanceService = attendanceService;
        _context = context;
    }

    public async Task<Manager> CreateManagerAsync(string firstName, string lastName, string phoneNumber, string nationalId,
                                                string email, string workId, string? password, User admin)
    {
        try
        {
            _logger.LogDebug("Creating manager: firstName={FirstName}, lastName={LastName}, email={Email}, workId={WorkId}", 
                firstName, lastName, email, workId);
            
            // Create user with MANAGER role
            var managerUser = await _userService.CreateUserAsync(firstName, lastName, phoneNumber, nationalId,
                                                             email, workId, password, Role.MANAGER, admin);
            
            _logger.LogDebug("Created user entity for manager with ID: {UserId}", managerUser.Id);
            
            // Try direct SQL insert first
            try
            {
                _logger.LogDebug("Attempting direct SQL insert for manager");
                var insertSql = "INSERT INTO managers (user_id, created_by) VALUES (@UserId, @CreatedBy)";
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(insertSql,
                    new Microsoft.Data.SqlClient.SqlParameter("@UserId", managerUser.Id),
                    new Microsoft.Data.SqlClient.SqlParameter("@CreatedBy", admin.Id));
                
                if (rowsAffected > 0)
                {
                    _logger.LogDebug("Successfully inserted manager record using SQL");
                }
                else
                {
                    _logger.LogWarning("SQL insert for manager returned 0 affected rows");
                }
                
                // Refresh entity manager to see the new record
                await _context.SaveChangesAsync();
            }
            catch (Exception sqlEx)
            {
                _logger.LogError(sqlEx, "SQL insert failed: {Message}", sqlEx.Message);
                // Fall back to repository
                try
                {
                    var manager = new Manager
                    {
                        UserId = managerUser.Id,
                        User = managerUser,
                        CreatedBy = admin
                    };
                    
                    _logger.LogDebug("Falling back to repository persist for manager entity");
                    await _managerRepository.AddAsync(manager);
                }
                catch (Exception repoEx)
                {
                    _logger.LogError(repoEx, "Repository persist also failed: {Message}", repoEx.Message);
                    throw;
                }
            }
            
            // Reload the manager from database
            var savedManager = await _managerRepository.GetByIdAsync(managerUser.Id);
            if (savedManager == null)
            {
                _logger.LogError("Failed to retrieve manager entity after persistence");
                throw new InvalidOperationException("Failed to retrieve manager entity after persistence");
            }
            
            _logger.LogDebug("Successfully created manager entity with ID: {ManagerId}", savedManager.UserId);
            
            // Log the action
            await _auditLogService.LogEventAsync(
                    "CREATE_MANAGER",
                    "MANAGER",
                    savedManager.UserId.ToString(),
                    admin,
                    $"Manager created: {email}"
            );
            
            return savedManager;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating manager: {Message}", e.Message);
            throw new InvalidOperationException($"Failed to create manager: {e.Message}", e);
        }
    }

    public async Task<Manager> GetManagerByIdAsync(long id)
    {
        var manager = await _managerRepository.GetByIdAsync(id);
        if (manager == null)
        {
            throw new InvalidOperationException($"Manager not found with ID: {id}");
        }
        return manager;
    }

    public async Task<IEnumerable<Manager>> GetAllManagersAsync()
    {
        return await _managerRepository.GetAllAsync();
    }

    public async Task<IEnumerable<Manager>> GetManagersByAdminAsync(User admin)
    {
        return await _managerRepository.GetByCreatedByAsync(admin);
    }

    public async Task DeleteManagerAsync(long id)
    {
        var manager = await GetManagerByIdAsync(id);
        var user = manager.User;
        
        // Log the action before deletion
        await _auditLogService.LogEventAsync(
                "DELETE_MANAGER",
                "MANAGER",
                id.ToString(),
                manager.CreatedBy, // Use the admin who created the manager
                $"Manager deleted: {user.Email}"
        );
        
        // Now delete/deactivate
        await _managerRepository.DeleteAsync(id);
        await _userService.UpdateUserStatusAsync(id, false); // Deactivate user instead of hard delete
        
        _logger.LogInformation("Manager with ID {ManagerId} has been deleted and user deactivated", id);
    }
    
    public async Task<Manager> UpdateManagerAsync(long id, Dictionary<string, object> updateFields)
    {
        var manager = await GetManagerByIdAsync(id);
        var user = manager.User;
        
        // Update user fields if provided
        if (updateFields.ContainsKey("firstName"))
        {
            user.FirstName = (string)updateFields["firstName"];
        }
        if (updateFields.ContainsKey("lastName"))
        {
            user.LastName = (string)updateFields["lastName"];
        }
        if (updateFields.ContainsKey("phoneNumber"))
        {
            user.PhoneNumber = (string)updateFields["phoneNumber"];
        }
        if (updateFields.ContainsKey("email"))
        {
            user.Email = (string)updateFields["email"];
        }
        
        // Save the manager
        var updatedManager = await _managerRepository.UpdateAsync(manager);
        
        // Log the action
        await _auditLogService.LogEventAsync(
                "UPDATE_MANAGER",
                "MANAGER",
                id.ToString(),
                manager.CreatedBy,
                $"Manager updated: {string.Join(", ", updateFields.Keys)}"
        );
        
        return updatedManager;
    }

    public async Task<Dictionary<string, object>> GetPerformanceOverviewAsync(Manager manager, string period)
    {
        // Determine date range based on period
        var today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly startDate;
        var endDate = today;
        
        startDate = period?.ToLower() switch
        {
            "monthly" => today.AddDays(1 - today.Day), // Current month: from 1st of current month to today
            "all_time" => new DateOnly(2000, 1, 1), // All time: from earliest possible date to today
            "weekly" or _ => today.AddDays(-(int)today.DayOfWeek + 1), // Current week: from Monday of current week to today
        };
        
        var startDateTime = startDate.ToDateTime(new TimeOnly(0, 0));
        var endDateTime = endDate.ToDateTime(new TimeOnly(23, 59, 59));
        
        _logger.LogDebug("Performance period: {Period}, Date range: {StartDate} to {EndDate}", period, startDate, endDate);

        // Get all agents and groups for this manager
        var agents = await _agentService.GetAgentsByManagerAsync(manager);
        var groups = await _groupService.GetGroupsByManagerAsync(manager);

        // Aggregate all clients for all agents
        var allClients = new List<Client>();
        foreach (var agent in agents)
        {
            var agentClients = await _clientService.GetClientsByAgentAndDateRangeAsync(agent, startDateTime, endDateTime);
            allClients.AddRange(agentClients);
        }

        _logger.LogDebug("Found {ClientCount} clients in date range {StartDate} to {EndDate}", allClients.Count, startDate, endDate);

        // Stats
        var stats = new Dictionary<string, object>
        {
            ["totalClients"] = allClients.Count
        };

        // Calculate most active location based on attendance records
        var allAttendances = new List<Attendance>();
        foreach (var agent in agents)
        {
            var agentAttendances = await _attendanceService.GetAttendanceByAgentAndDateRangeAsync(agent, startDateTime, endDateTime);
            allAttendances.AddRange(agentAttendances);
        }
        
        var mostActiveLocation = "Kigali"; // Default location when no data
        if (allAttendances.Any())
        {
            var locationCounts = allAttendances
                .Where(att => !string.IsNullOrWhiteSpace(att.Location) && att.Location.Trim() != "Unknown Location")
                .GroupBy(att => att.Location.Trim())
                .ToDictionary(g => g.Key, g => g.Count());
            
            if (locationCounts.Any())
            {
                mostActiveLocation = locationCounts.MaxBy(kvp => kvp.Value).Key;
            }
        }
        
        _logger.LogDebug("Most active location calculation: {AttendanceCount} attendances found, most active: {Location}", 
                 allAttendances.Count, mostActiveLocation);
        
        // Debug: Log all attendance locations
        if (allAttendances.Any())
        {
            _logger.LogDebug("All attendance locations: {Locations}", 
                string.Join(", ", allAttendances.Select(att => $"{att.Location} (agent: {att.Agent.User.FirstName})")));
        }
        
        stats["mostActiveLocation"] = mostActiveLocation;

        // Group Performance
        var groupPerformance = new List<Dictionary<string, object>>();
        foreach (var group in groups)
        {
            var groupAgents = group.Agents.ToList();
            var leader = group.Leader;
            var groupClients = groupAgents.Sum(agent => 
                allClients.Count(c => c.Agent.UserId == agent.UserId));
            
            var membersList = groupAgents.Select(agent => new Dictionary<string, object>
            {
                ["name"] = $"{agent.User.FirstName} {agent.User.LastName}",
                ["clients"] = allClients.Count(c => c.Agent.UserId == agent.UserId)
            }).ToList();
            
            var groupMap = new Dictionary<string, object>
            {
                ["name"] = group.Name,
                ["teamLeader"] = (leader != null ? $"{leader.User.FirstName} {leader.User.LastName}" : null) ?? throw new InvalidOperationException(),
                ["members"] = groupAgents.Count,
                ["clients"] = groupClients,
                ["membersList"] = membersList
            };
            
            groupPerformance.Add(groupMap);
        }

        // Individual Performance
        var individualPerformance = agents.Select(agent => new Dictionary<string, object>
        {
            ["name"] = $"{agent.User.FirstName} {agent.User.LastName}",
            ["clients"] = allClients.Count(c => c.Agent.UserId == agent.UserId)
        }).ToList();

        // Clients Collected
        var clientsCollected = allClients.Select(client => new Dictionary<string, object>
        {
            ["fullName"] = client.FullName,
            ["nationalId"] = client.NationalId,
            ["phoneNumber"] = client.PhoneNumber,
            ["location"] = client.Location,
            ["dateOfBirth"] = client.DateOfBirth.ToString("yyyy-MM-dd"),
            ["insuranceType"] = client.InsuranceType,
            ["payingAmount"] = client.PayingAmount,
            ["payingMethod"] = client.PayingMethod,
            ["contractYears"] = client.ContractYears,
            ["agentName"] = (client.Agent?.User != null ? $"{client.Agent.User.FirstName} {client.Agent.User.LastName}" : null) ?? throw new InvalidOperationException(),
            ["agentWorkLocation"] = client.Agent?.Sector ?? throw new InvalidOperationException()
        }).ToList();

        var response = new Dictionary<string, object>
        {
            ["stats"] = stats,
            ["groupPerformance"] = groupPerformance,
            ["individualPerformance"] = individualPerformance,
            ["clientsCollected"] = clientsCollected
        };
        
        return response;
    }

    public async Task<(IEnumerable<Dictionary<string, object>> Clients, int TotalCount)> GetClientsCollectedAsync(Manager manager, string? search, int page, int pageSize)
    {
        // Get all agents for this manager
        var agents = await _agentService.GetAgentsByManagerAsync(manager);
        var allClients = new List<Client>();
        
        foreach (var agent in agents)
        {
            var agentClients = await _clientService.GetClientsByAgentAsync(agent);
            allClients.AddRange(agentClients);
        }
        
        // Filter by search if provided
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lowerSearch = search.ToLower();
            allClients = allClients.Where(c =>
                (c.FullName != null && c.FullName.ToLower().Contains(lowerSearch)) ||
                (c.NationalId != null && c.NationalId.ToLower().Contains(lowerSearch))
            ).ToList();
        }
        
        var total = allClients.Count;
        var start = Math.Min((page - 1) * pageSize, total);
        var end = Math.Min(start + pageSize, total);
        
        var pageContent = allClients.Skip(start).Take(end - start).Select(client => new Dictionary<string, object>
        {
            ["fullName"] = client.FullName,
            ["nationalId"] = client.NationalId,
            ["phoneNumber"] = client.PhoneNumber,
            ["location"] = client.Location,
            ["agentName"] = $"{client.Agent.User.FirstName} {client.Agent.User.LastName}"
        }).ToList();
        
        return (pageContent, total);
    }
}