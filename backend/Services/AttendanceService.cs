using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class AttendanceService : IAttendanceService
{
    private readonly ILogger<AttendanceService> _logger;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IAttendanceTimeframeRepository _attendanceTimeframeRepository;
    private readonly IAuditLogService _auditLogService;

    public AttendanceService(
        ILogger<AttendanceService> logger,
        IAttendanceRepository attendanceRepository,
        IAttendanceTimeframeRepository attendanceTimeframeRepository,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _attendanceRepository = attendanceRepository;
        _attendanceTimeframeRepository = attendanceTimeframeRepository;
        _auditLogService = auditLogService;
    }

    public async Task<Attendance> MarkAttendanceAsync(Agent agent, string location, string sector)
    {
        // Check if agent has already marked attendance today
        if (await HasMarkedAttendanceTodayAsync(agent))
        {
            throw new InvalidOperationException("Attendance already marked today");
        }
        
        // Get the attendance timeframe for the manager
        var timeframes = await _attendanceTimeframeRepository.GetByManagerAsync(agent.Manager);
        var timeframe = timeframes.FirstOrDefault();
        
        var startTime = timeframe?.StartTime ?? new TimeOnly(6, 0); // Default 6:00 AM
        var endTime = timeframe?.EndTime ?? new TimeOnly(9, 0); // Default 9:00 AM
        
        // Check if current time is within timeframe
        var now = TimeOnly.FromDateTime(DateTime.Now);
        if (now < startTime || now > endTime)
        {
            throw new InvalidOperationException($"Attendance must be marked between {startTime} and {endTime}");
        }
        
        // Validate location and sector
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new InvalidOperationException("Location is required");
        }
        if (string.IsNullOrWhiteSpace(sector))
        {
            throw new InvalidOperationException("Sector is required");
        }
        
        // Create and save attendance
        var attendance = new Attendance
        {
            Agent = agent,
            Timestamp = DateTime.Now,
            Location = location.Trim(),
            Sector = sector.Trim()
        };
        
        var savedAttendance = await _attendanceRepository.AddAsync(attendance);
        
        // Log the action
        await _auditLogService.LogEventAsync(
                "MARK_ATTENDANCE",
                "ATTENDANCE",
                savedAttendance.Id.ToString(),
                agent.User,
                $"Attendance marked at {savedAttendance.Location} in sector {savedAttendance.Sector}"
        );
        
        return savedAttendance;
    }

    public async Task<IEnumerable<Attendance>> GetAttendanceByAgentAsync(Agent agent)
    {
        return await _attendanceRepository.GetByAgentAsync(agent);
    }

    public async Task<IEnumerable<Attendance>> GetAttendanceByAgentAndDateRangeAsync(Agent agent, DateTime start, DateTime end)
    {
        return await _attendanceRepository.GetByAgentAndDateRangeAsync(agent, start, end);
    }

    public async Task<bool> HasMarkedAttendanceTodayAsync(Agent agent)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var startOfDay = today.ToDateTime(new TimeOnly(0, 0));
        var endOfDay = today.ToDateTime(new TimeOnly(23, 59, 59));
        
        var attendances = await _attendanceRepository.GetByAgentAndDateRangeAsync(agent, startOfDay, endOfDay);
        return attendances.Any();
    }

    public async Task<DateTime?> GetLastAttendanceTimeAsync(Agent agent)
    {
        var attendances = await _attendanceRepository.GetByAgentAsync(agent);
        return attendances.OrderByDescending(a => a.Timestamp).FirstOrDefault()?.Timestamp;
    }
}