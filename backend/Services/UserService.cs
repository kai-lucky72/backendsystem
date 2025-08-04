using backend.DTOs;
using backend.DTOs.Admin;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly PasswordHasher<User> _passwordHasher;

    public UserService(
        ILogger<UserService> logger,
        IUserRepository userRepository,
        IAgentRepository agentRepository,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _userRepository = userRepository;
        _agentRepository = agentRepository;
        _auditLogService = auditLogService;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<User> CreateUserAsync(string firstName, string lastName, string phoneNumber, string nationalId,
                                          string email, string workId, string? password, Role role, User createdBy)
    {
        if (await _userRepository.ExistsByEmailAsync(email))
        {
            throw new ArgumentException("Email already exists");
        }
        if (await _userRepository.ExistsByWorkIdAsync(workId))
        {
            throw new ArgumentException("Work ID already exists");
        }
        if (await _userRepository.ExistsByPhoneNumberAsync(phoneNumber))
        {
            throw new ArgumentException("Phone number already exists");
        }
        if (await _userRepository.ExistsByNationalIdAsync(nationalId))
        {
            throw new ArgumentException("National ID already exists");
        }

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            NationalId = nationalId,
            Email = email,
            WorkId = workId,
            PasswordHash = password != null ? _passwordHasher.HashPassword(null, password) : null,
            Role = role,
            Active = true
        };

        var savedUser = await _userRepository.AddAsync(user);
        
        // Log the action
        await _auditLogService.LogEventAsync(
                "CREATE_USER",
                "USER",
                savedUser.Id.ToString(),
                createdBy,
                $"User created: {email}, Role: {role}"
        );

        return savedUser;
    }

    public async Task<User> GetUserByIdAsync(long id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new InvalidOperationException($"User not found with ID: {id}");
        }
        return user;
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            throw new InvalidOperationException($"User not found with email: {email}");
        }
        return user;
    }

    public async Task<User> GetUserByWorkIdAsync(string workId)
    {
        var user = await _userRepository.GetByWorkIdAsync(workId);
        if (user == null)
        {
            throw new InvalidOperationException($"User not found with work ID: {workId}");
        }
        return user;
    }

public async Task<IEnumerable<User>> GetAllUsersAsync()
{
    _logger.LogDebug("Retrieving all users from database");
    var allUsers = await _userRepository.GetAllAsync();
    _logger.LogDebug("Found {UserCount} users in database", allUsers.Count());
    
    // Log details about each user for debugging
    foreach (var user in allUsers)
    {
        _logger.LogDebug("User: id={UserId}, workId={WorkId}, email={Email}, role={Role}, active={Active}", 
                 user.Id, user.WorkId, user.Email, user.Role, user.Active);
    }
    
    return allUsers;
}

    public async Task<int> CountUsersByRoleAsync(Role role)
    {
        var allUsers = await _userRepository.GetAllAsync();
        return allUsers.Count(u => u.Role == role);
    }
    
    public async Task<bool> IsEmailTakenAsync(string email)
    {
        return await _userRepository.ExistsByEmailAsync(email);
    }
    
    public async Task<bool> IsWorkIdTakenAsync(string workId)
    {
        return await _userRepository.ExistsByWorkIdAsync(workId);
    }

    public async Task<bool> IsPhoneNumberTakenAsync(string phoneNumber)
    {
        return await _userRepository.ExistsByPhoneNumberAsync(phoneNumber);
    }

    public async Task<AdminDashboardDTO> GetAdminDashboardAsync()
    {
        var allUsers = await _userRepository.GetAllAsync();
        var managers = allUsers.Where(u => u.Role == Role.MANAGER).ToList();
        var agents = allUsers.Where(u => u.Role == Role.AGENT).ToList();
        
        var systemMetrics = new List<AdminDashboardDTO.SystemMetric>
        {
            new() { Name = "Total Users", Users = allUsers.Count(), Activity = allUsers.Count(u => u.Active) },
            new() { Name = "Managers", Users = managers.Count(), Activity = managers.Count(u => u.Active) },
            new() { Name = "Agents", Users = agents.Count(), Activity = agents.Count(u => u.Active) }
        };

        var userActivity = new AdminDashboardDTO.UserActivityModel
        {
            Managers = new() { Count = managers.Count(), Change = "+5%" },
            Agents = new() { Count = agents.Count(), Change = "+12%" },
            ActiveToday = new() { Count = allUsers.Count(u => u.Active), Change = "+8%" },
            NotificationsSent = new() { Count = 0, Change = "+15%" }
        };

        var recentActivities = new List<AdminDashboardDTO.RecentSystemActivity>
        {
            new() { Action = "User Created", User = "Admin", Time = "2 minutes ago" },
            new() { Action = "Manager Added", User = "System", Time = "5 minutes ago" },
            new() { Action = "Agent Activated", User = "Admin", Time = "10 minutes ago" }
        };

        return new AdminDashboardDTO
        {
            SystemMetrics = systemMetrics,
            UserActivity = userActivity,
            RecentSystemActivities = recentActivities
        };
    }
    
    public async Task<IEnumerable<User>> GetUsersByRoleAsync(Role role)
    {
        var allUsers = await _userRepository.GetAllAsync();
        return allUsers.Where(u => u.Role == role);
    }


    public async Task<User> UpdateUserStatusAsync(long id, bool active)
    {
        var user = await GetUserByIdAsync(id);
        user.Active = active;
        
        var savedUser = await _userRepository.UpdateAsync(user);
        
        await _auditLogService.LogEventAsync(
                active ? "ACTIVATE_USER" : "DEACTIVATE_USER",
                "USER",
                id.ToString(),
                savedUser,
                $"User {(active ? "activated" : "deactivated")}: {user.Email}"
        );
        
        return savedUser;
    }

    public async Task<bool> ResetPasswordAsync(long id, string newPassword)
    {
        var user = await GetUserByIdAsync(id);
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        await _userRepository.UpdateAsync(user);
        
        await _auditLogService.LogEventAsync(
                "RESET_PASSWORD",
                "USER",
                id.ToString(),
                user,
                $"Password reset for: {user.Email}"
        );
        
        return true;
    }

    private UserDTO MapToDTO(User user)
    {
        var builder = new UserDTO
        {
            Id = $"usr-{user.Id:D3}",
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber ?? throw new InvalidOperationException(message:"null value for phonenumber in userservice"),
            NationalId = user.NationalId ?? throw new InvalidOperationException(message:"null value for nationalid in userservice"),
            Email = user.Email,
            WorkId = user.WorkId,
            Role = user.Role,
            CreatedAt = UserDTO.FormatDate(user.CreatedAt) ?? throw new InvalidOperationException(message:"null value for createdat in userservice"),
            Active = user.Active,
            Status = user.Active ? "active" : "inactive"
        };
        
        
        
        if (user.Role == Role.AGENT)
        {
            var agent = _agentRepository.GetByUserIdAsync(user.Id).Result;
            if (agent != null)
            {
                builder.Type = agent.AgentType.ToString().ToLower();
                builder.Sector = agent.Sector;
                builder.Group = agent.Group?.Name;
            }
        }
        
        return builder;
    }
    
    public Task<int> GetUserCountByRoleAsync(string roleName)
    {
        if (!Enum.TryParse<Role>(roleName, true, out var role))
            throw new ArgumentException($"Invalid role: {roleName}");
        return CountUsersByRoleAsync(role);
    }

    public Task<int> GetUserCountForMonthAsync(DateTime monthStart) =>
        _userRepository.CountByCreatedAtBetweenAsync(
            monthStart,
            monthStart.AddMonths(1).AddSeconds(-1)
        );
}