using backend.Models;

namespace backend.Repositories;

public interface IAttendanceTimeframeRepository
{
    Task<IEnumerable<AttendanceTimeframe>> GetAllAsync();
    Task<AttendanceTimeframe?> GetByIdAsync(long id);
    Task<AttendanceTimeframe> AddAsync(AttendanceTimeframe attendanceTimeframe);
    Task<AttendanceTimeframe> UpdateAsync(AttendanceTimeframe attendanceTimeframe);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<IEnumerable<AttendanceTimeframe>> GetByManagerIdAsync(long managerId);
    Task<AttendanceTimeframe?> GetByManagerAsync(Manager manager);

    // New: latest timeframe irrespective of manager (global setting)
    Task<AttendanceTimeframe?> GetLatestAsync();
}