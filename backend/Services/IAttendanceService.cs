using backend.Models;

namespace backend.Services;

public interface IAttendanceService
{
    Task<Attendance> MarkAttendanceAsync(Agent agent, string location, string sector);
    
    Task<IEnumerable<Attendance>> GetAttendanceByAgentAsync(Agent agent);
    
    Task<IEnumerable<Attendance>> GetAttendanceByAgentAndDateRangeAsync(Agent agent, DateTime start, DateTime end);
    
    Task<bool> HasMarkedAttendanceTodayAsync(Agent agent);
    
    Task<DateTime?> GetLastAttendanceTimeAsync(Agent agent);
}