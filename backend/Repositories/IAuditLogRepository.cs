using backend.Models;

namespace backend.Repositories;

public interface IAuditLogRepository
{
    Task<IEnumerable<AuditLog>> GetAllAsync();
    Task<AuditLog?> GetByIdAsync(long id);
    Task<AuditLog> AddAsync(AuditLog auditLog);
    Task<AuditLog> UpdateAsync(AuditLog auditLog);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<IEnumerable<AuditLog>> GetByUserOrderByTimestampDescAsync(User user);
    Task<IEnumerable<AuditLog>> GetByUserIdOrderByTimestampDescAsync(long userId);
    Task<IEnumerable<AuditLog>> GetTop5ByOrderByTimestampDescAsync();
    Task<IEnumerable<AuditLog>> GetByEntityTypeAndEntityIdOrderByTimestampDescAsync(string entityType, string entityId);
    Task<IEnumerable<AuditLog>> GetByTimestampBetweenOrderByTimestampDescAsync(DateTime start, DateTime end);
    Task<IEnumerable<AuditLog>> GetByEventTypeOrderByTimestampDescAsync(string eventType);
    Task<IEnumerable<AuditLog>> GetAllByOrderByTimestampDescAsync(int page, int pageSize);
    Task<int> CountByTimestampBetweenAsync(DateTime start, DateTime end);
    Task<int> CountByEventTypeAsync(string eventType);
    Task<int> CountByEventTypeAndTimestampBetweenAsync(string eventType, DateTime start, DateTime end);
    Task<IEnumerable<AuditLog>> SearchLogsAsync(string? eventType, string? entityType, string? entityId, string? details, DateTime startDate, DateTime endDate);
}