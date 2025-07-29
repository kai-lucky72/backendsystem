using backend.Models;

namespace backend.Services;

public interface IAttendanceTimeframeService
{
    Task<AttendanceTimeframe> SetTimeframeAsync(Manager manager, TimeOnly startTime, TimeOnly endTime);
    
    Task<AttendanceTimeframe?> GetTimeframeByManagerAsync(Manager manager);
    
    Task<AttendanceTimeframe> UpdateTimeframeAsync(Manager manager, TimeOnly startTime, TimeOnly endTime);
}