using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class AttendanceTimeframeRepository : IAttendanceTimeframeRepository
{
    private readonly ApplicationDbContext _context;

    public AttendanceTimeframeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AttendanceTimeframe>> GetAllAsync()
    {
        return await _context.AttendanceTimeframes
            .Include(at => at.Manager)
            .ThenInclude(m => m!.User)
            .ToListAsync();
    }

    public async Task<AttendanceTimeframe?> GetByIdAsync(long id)
    {
        return await _context.AttendanceTimeframes
            .Include(at => at.Manager)
            .ThenInclude(m => m!.User)
            .FirstOrDefaultAsync(at => at.Id == id);
    }

    public async Task<AttendanceTimeframe> AddAsync(AttendanceTimeframe attendanceTimeframe)
    {
        await _context.AttendanceTimeframes.AddAsync(attendanceTimeframe);
        await _context.SaveChangesAsync();
        return attendanceTimeframe;
    }

    public async Task<AttendanceTimeframe> UpdateAsync(AttendanceTimeframe attendanceTimeframe)
    {
        _context.AttendanceTimeframes.Update(attendanceTimeframe);
        await _context.SaveChangesAsync();
        return attendanceTimeframe;
    }

    public async Task DeleteAsync(long id)
    {
        var attendanceTimeframe = await GetByIdAsync(id);
        if (attendanceTimeframe != null)
        {
            _context.AttendanceTimeframes.Remove(attendanceTimeframe);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<AttendanceTimeframe>> GetByManagerIdAsync(long managerId)
    {
        return await _context.AttendanceTimeframes
            .Include(at => at.Manager)
            .ThenInclude(m => m!.User)
            .Where(at => at.ManagerId == managerId)
            .ToListAsync();
    }

    public async Task<AttendanceTimeframe?> GetByManagerAsync(Manager manager)
    {
        return await _context.AttendanceTimeframes
            .Include(at => at.Manager)
            .ThenInclude(m => m!.User)
            .FirstOrDefaultAsync(at => at.ManagerId == manager.UserId);
    }
}