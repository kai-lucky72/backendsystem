using backend.Models;

namespace backend.Repositories;

public interface IAttendanceRepository
{
    Task<IEnumerable<Attendance>> GetAllAsync();
    Task<Attendance?> GetByIdAsync(long id);
    Task<Attendance> AddAsync(Attendance attendance);
    Task<Attendance> UpdateAsync(Attendance attendance);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<IEnumerable<Attendance>> GetByAgentOrderByTimestampDescAsync(Agent agent);
    Task<IEnumerable<Attendance>> GetByAgentAndDateRangeAsync(long agentId, DateTime startDate, DateTime endDate);
    Task<Attendance?> GetFirstByAgentAndTimestampBetweenOrderByTimestampDescAsync(long agentId, DateTime startDateTime, DateTime endDateTime);
}