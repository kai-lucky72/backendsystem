using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AuditLog>> GetAllAsync()
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }

    public async Task<AuditLog?> GetByIdAsync(long id)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .FirstOrDefaultAsync(al => al.Id == id);
    }

    public async Task<AuditLog> AddAsync(AuditLog auditLog)
    {
        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
        return auditLog;
    }

    public async Task<AuditLog> UpdateAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Update(auditLog);
        await _context.SaveChangesAsync();
        return auditLog;
    }

    public async Task DeleteAsync(long id)
    {
        var auditLog = await GetByIdAsync(id);
        if (auditLog != null)
        {
            _context.AuditLogs.Remove(auditLog);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<AuditLog>> GetByUserOrderByTimestampDescAsync(User user)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.UserId == user.Id)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdOrderByTimestampDescAsync(long userId)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetTop5ByOrderByTimestampDescAsync()
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .OrderByDescending(al => al.Timestamp)
            .Take(5)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityTypeAndEntityIdOrderByTimestampDescAsync(string entityType, string entityId)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.EntityType == entityType && al.EntityId == entityId)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByTimestampBetweenOrderByTimestampDescAsync(DateTime start, DateTime end)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.Timestamp >= start && al.Timestamp <= end)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByEventTypeOrderByTimestampDescAsync(string eventType)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.EventType == eventType)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAllByOrderByTimestampDescAsync(int page, int pageSize)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .OrderByDescending(al => al.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByTimestampBetweenAsync(DateTime start, DateTime end)
    {
        return await _context.AuditLogs
            .CountAsync(al => al.Timestamp >= start && al.Timestamp <= end);
    }

    public async Task<int> CountByEventTypeAsync(string eventType)
    {
        return await _context.AuditLogs
            .CountAsync(al => al.EventType == eventType);
    }

    public async Task<int> CountByEventTypeAndTimestampBetweenAsync(string eventType, DateTime start, DateTime end)
    {
        return await _context.AuditLogs
            .CountAsync(al => al.EventType == eventType && 
                             al.Timestamp >= start && 
                             al.Timestamp <= end);
    }

    public async Task<IEnumerable<AuditLog>> SearchLogsAsync(string? eventType, string? entityType, string? entityId, string? details, DateTime startDate, DateTime endDate)
    {
        var query = _context.AuditLogs.Include(al => al.User).AsQueryable();

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(al => al.EventType == eventType);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(al => al.EntityType == entityType);

        if (!string.IsNullOrEmpty(entityId))
            query = query.Where(al => al.EntityId == entityId);

        if (!string.IsNullOrEmpty(details))
            query = query.Where(al => al.Details != null && al.Details.Contains(details));

        return await query
            .Where(al => al.Timestamp >= startDate && al.Timestamp <= endDate)
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByUserAsync(User user)
    {
        return await GetByUserOrderByTimestampDescAsync(user);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityTypeAndEntityIdAsync(string entityType, string entityId)
    {
        return await GetByEntityTypeAndEntityIdOrderByTimestampDescAsync(entityType, entityId);
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(long userId)
    {
        return await GetByUserIdOrderByTimestampDescAsync(userId);
    }

    // MISSING METHODS IMPLEMENTATION
    
    public async Task<int> CountByEventTypeAndDateAsync(string eventType, DateTime date)
    {
        // Count events of a specific type on a specific date (full day)
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);
        
        return await _context.AuditLogs
            .CountAsync(al => al.EventType == eventType && 
                             al.Timestamp >= startOfDay && 
                             al.Timestamp <= endOfDay);
    }

    public async Task<int> CountByMetricTypePreviousPeriodAsync(string metricType)
    {
        // This seems to be for analytics - counting events in previous period
        // Assuming we want to count events of this type in the previous month
        var now = DateTime.UtcNow;
        var previousMonthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
        var previousMonthEnd = previousMonthStart.AddMonths(1).AddTicks(-1);
        
        return await _context.AuditLogs
            .CountAsync(al => al.EventType == metricType && 
                             al.Timestamp >= previousMonthStart && 
                             al.Timestamp <= previousMonthEnd);
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count)
    {
        // Get the most recent audit logs
        return await _context.AuditLogs
            .Include(al => al.User)
            .OrderByDescending(al => al.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}