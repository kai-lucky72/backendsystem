using System.Linq;
using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class AttendanceTimeframeService : IAttendanceTimeframeService
{
    private readonly ILogger<AttendanceTimeframeService> _logger;
    private readonly IAttendanceTimeframeRepository _attendanceTimeframeRepository;
    private readonly IAuditLogService _auditLogService;

    public AttendanceTimeframeService(
        ILogger<AttendanceTimeframeService> logger,
        IAttendanceTimeframeRepository attendanceTimeframeRepository,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _attendanceTimeframeRepository = attendanceTimeframeRepository;
        _auditLogService = auditLogService;
    }

    public async Task<AttendanceTimeframe> SetTimeframeAsync(Manager manager, TimeOnly startTime, TimeOnly endTime)
    {
        ValidateTimeframe(startTime, endTime);
        
        // Check if timeframe already exists
        var existingTimeframe = await GetTimeframeByManagerAsync(manager);
        
        if (existingTimeframe != null)
        {
            return await UpdateTimeframeAsync(manager, startTime, endTime);
        }
        
        // Default to Monday
        var defaultDayOfWeek = (byte)(DayOfWeek.Monday);
        
        var timeframe = new AttendanceTimeframe
        {
            ManagerId = manager.UserId,
            Manager = manager,
            DayOfWeek = defaultDayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            BreakDuration = 60, // Default 60 minutes break
            AppliesToAllAgents = true // Default applies to all agents
        };
        
        var savedTimeframe = await _attendanceTimeframeRepository.AddAsync(timeframe);
        
        await _auditLogService.LogEventAsync(
                "SET_ATTENDANCE_TIMEFRAME",
                "TIMEFRAME",
                manager.UserId.ToString(),
                manager.User,
                $"Attendance timeframe set: {startTime} - {endTime}"
        );
        
        return savedTimeframe;
    }

    public async Task<AttendanceTimeframe?> GetTimeframeByManagerAsync(Manager manager)
    {
        return await _attendanceTimeframeRepository.GetByManagerAsync(manager);
    }

    public async Task<AttendanceTimeframe> UpdateTimeframeAsync(Manager manager, TimeOnly startTime, TimeOnly endTime)
    {
        ValidateTimeframe(startTime, endTime);
        
        var timeframe = await GetTimeframeByManagerAsync(manager);
        if (timeframe == null)
        {
            throw new InvalidOperationException("Timeframe not found for manager");
        }
        
        timeframe.StartTime = startTime;
        timeframe.EndTime = endTime;
        
        var updatedTimeframe = await _attendanceTimeframeRepository.UpdateAsync(timeframe);
        
        await _auditLogService.LogEventAsync(
                "UPDATE_ATTENDANCE_TIMEFRAME",
                "TIMEFRAME",
                manager.UserId.ToString(),
                manager.User,
                $"Attendance timeframe updated: {startTime} - {endTime}"
        );
        
        return updatedTimeframe;
    }
    
    private void ValidateTimeframe(TimeOnly startTime, TimeOnly endTime)
    {
        if (startTime >= endTime)
        {
            throw new InvalidOperationException("Start time must be before end time");
        }
    }
}