using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly ApplicationDbContext _context;

    public AttendanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Attendance>> GetAllAsync()
    {
        return await _context.Attendances
            .Include(a => a.Agent)
            .ThenInclude(agent => agent!.User)
            .ToListAsync();
    }

    public async Task<Attendance?> GetByIdAsync(long id)
    {
        return await _context.Attendances
            .Include(a => a.Agent)
            .ThenInclude(agent => agent!.User)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Attendance> AddAsync(Attendance attendance)
    {
        await _context.Attendances.AddAsync(attendance);
        await _context.SaveChangesAsync();
        return attendance;
    }

    public async Task<Attendance> UpdateAsync(Attendance attendance)
    {
        _context.Attendances.Update(attendance);
        await _context.SaveChangesAsync();
        return attendance;
    }

    public async Task DeleteAsync(long id)
    {
        var attendance = await GetByIdAsync(id);
        if (attendance != null)
        {
            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Attendance>> GetByAgentOrderByTimestampDescAsync(Agent agent)
    {
        return await _context.Attendances
            .Include(a => a.Agent)
            .ThenInclude(agent => agent!.User)
            .Where(a => a.AgentId == agent.UserId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<Attendance>> GetByAgentAndDateRangeAsync(Agent agent, DateTime startDate, DateTime endDate)
    {
        return await _context.Attendances
            .Include(a => a.Agent)
            .ThenInclude(agent => agent!.User)
            .Where(a => a.AgentId == agent.UserId && 
                       a.Timestamp >= startDate && 
                       a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<Attendance?> GetFirstByAgentAndTimestampBetweenOrderByTimestampDescAsync(Agent agent, DateTime startDateTime, DateTime endDateTime)
    {
        return await _context.Attendances
            .Include(a => a.Agent)
            .ThenInclude(agent => agent!.User)
            .Where(a => a.AgentId == agent.UserId && 
                       a.Timestamp >= startDateTime && 
                       a.Timestamp <= endDateTime)
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();
    }
}