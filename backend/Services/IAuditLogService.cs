using backend.Models;

namespace backend.Services;

public interface IAuditLogService
{
    /// <summary>
    /// Log an event with the current user
    /// </summary>
    Task<AuditLog> LogEventAsync(string action, string entityType, string entityId, User user, string details);
    
    /// <summary>
    /// Log an event with the current user and specified IP address
    /// </summary>
    Task<AuditLog> LogEventAsync(string action, string entityType, string entityId, User user, string details, string? ipAddress);
    
    /// <summary>
    /// Log a system-generated event (no user associated)
    /// </summary>
    Task LogSystemEventAsync(string action, string entityType, string entityId, string details);
    
    /// <summary>
    /// Get logs for a specific user
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsByUserAsync(User user);
    
    /// <summary>
    /// Get logs for a specific entity
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsByEntityTypeAndIdAsync(string entityType, string entityId);
    
    /// <summary>
    /// Get logs within a date range
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsByDateRangeAsync(DateTime start, DateTime end);
    
    /// <summary>
    /// Get logs for a specific action type
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsByActionAsync(string action);
    
    Task<IEnumerable<AuditLog>> GetAllLogsAsync();
    Task<int> GetActivityCountForMonthAsync(DateTime monthStart);
    Task<int> GetActiveTodayCountAsync();
    Task<int> GetPreviousPeriodCountAsync(string metricType);
    Task<IEnumerable<AuditLog>> GetRecentActivitiesAsync(int count);

    
    /// <summary>
    /// Get paginated logs
    /// </summary>
    Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetAllLogsPaginatedAsync(int page, int pageSize);
    
    /// <summary>
    /// Search logs with complex filtering
    /// </summary>
    Task<IEnumerable<AuditLog>> SearchLogsAsync(Dictionary<string, string> filters, DateTime start, DateTime end);
    
    /// <summary>
    /// Clear the audit log cache
    /// </summary>
    Task ClearAuditLogCacheAsync();

    /// <summary>
    /// Get paginated logs with sorting
    /// </summary>
    Task<(IEnumerable<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(int page, int pageSize, string? sortBy = null, bool ascending = true);
    
    /// <summary>
    /// Get a specific log by ID
    /// </summary>
    Task<AuditLog?> GetLogByIdAsync(long id);
    
    /// <summary>
    /// Get logs for a specific user ID
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsByUserIdAsync(long userId);
}