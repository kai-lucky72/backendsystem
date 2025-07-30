using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ILogger<AuditLogService> _logger;
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(
        ILogger<AuditLogService> logger,
        IAuditLogRepository auditLogRepository)
    {
        _logger = logger;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<AuditLog> LogEventAsync(string action, string entityType, string entityId, User user, string details)
    {
        return await LogEventAsync(action, entityType, entityId, user, details, null);
    }

    public async Task<AuditLog> LogEventAsync(string action, string entityType, string entityId, User user, string details, string? ipAddress)
    {
        var auditLog = new AuditLog
        {
            EventType = action, // Using action as eventType
            EntityType = entityType,
            EntityId = entityId,
            User = user,
            Details = details,
            Timestamp = DateTime.Now
        };
        
        return await _auditLogRepository.AddAsync(auditLog);
    }

    public async Task LogSystemEventAsync(string action, string entityType, string entityId, string details)
    {
        // Create a system-generated audit log entry
        var auditLog = new AuditLog
        {
            EventType = action, // Using action as eventType
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.Now
        };
        
        await _auditLogRepository.AddAsync(auditLog);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByUserAsync(User user)
    {
        return await _auditLogRepository.GetByUserAsync(user);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByEntityTypeAndIdAsync(string entityType, string entityId)
    {
        return await _auditLogRepository.GetByEntityTypeAndEntityIdAsync(entityType, entityId);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByDateRangeAsync(DateTime start, DateTime end)
    {
        return await _auditLogRepository.GetByTimestampBetweenOrderByTimestampDescAsync(start, end);
    }
    
    public async Task<IEnumerable<AuditLog>> GetLogsByActionAsync(string action)
    {
        return await _auditLogRepository.GetByEventTypeOrderByTimestampDescAsync(action);
    }
    
    public async Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetAllLogsPaginatedAsync(int page, int pageSize)
    {
        var allLogs = await _auditLogRepository.GetAllAsync();
        var totalCount = allLogs.Count();
        var logs = allLogs.OrderByDescending(l => l.Timestamp)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize);
        
        return (logs, totalCount);
    }
    
    public async Task<IEnumerable<AuditLog>> SearchLogsAsync(Dictionary<string, string> filters, DateTime start, DateTime end)
    {
        // Extract filter values
        var eventType = filters.GetValueOrDefault("action", null); // Map "action" to "eventType"
        var entityType = filters.GetValueOrDefault("entityType", null);
        var entityId = filters.GetValueOrDefault("entityId", null);
        var details = filters.GetValueOrDefault("details", null);
        
        // Use the repository method for efficient database querying
        return await _auditLogRepository.SearchLogsAsync(eventType, entityType, entityId, details, start, end);
    }
    
    public async Task ClearAuditLogCacheAsync()
    {
        // Method implementation is empty as caching will be handled by a separate caching service
        // This method is meant to be called explicitly when needed to clear the cache
        await Task.CompletedTask;
    }
    
    public async Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(int page, int pageSize, string? sortBy = null, bool ascending = true)
    {
        var allLogs = await _auditLogRepository.GetAllAsync();
        var totalCount = allLogs.Count();
        
        var query = allLogs.AsQueryable();
        
        // Apply sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            query = sortBy.ToLower() switch
            {
                "timestamp" => ascending ? query.OrderBy(l => l.Timestamp) : query.OrderByDescending(l => l.Timestamp),
                "eventtype" => ascending ? query.OrderBy(l => l.EventType) : query.OrderByDescending(l => l.EventType),
                "entitytype" => ascending ? query.OrderBy(l => l.EntityType) : query.OrderByDescending(l => l.EntityType),
                "entityid" => ascending ? query.OrderBy(l => l.EntityId) : query.OrderByDescending(l => l.EntityId),
                _ => query.OrderByDescending(l => l.Timestamp) // Default sorting
            };
        }
        else
        {
            query = query.OrderByDescending(l => l.Timestamp);
        }
        
        var logs = query.Skip((page - 1) * pageSize).Take(pageSize);
        
        return (logs, totalCount);
    }

    public async Task<AuditLog?> GetLogByIdAsync(long id)
    {
        return await _auditLogRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByUserIdAsync(long userId)
    {
        return await _auditLogRepository.GetByUserIdAsync(userId);
    }
}