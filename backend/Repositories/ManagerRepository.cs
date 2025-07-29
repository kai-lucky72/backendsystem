using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class ManagerRepository : IManagerRepository
{
    private readonly ApplicationDbContext _context;

    public ManagerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Manager>> GetAllAsync()
    {
        return await _context.Managers
            .Include(m => m.User)
            .Include(m => m.CreatedBy)
            .ToListAsync();
    }

    public async Task<Manager?> GetByIdAsync(long id)
    {
        return await _context.Managers
            .Include(m => m.User)
            .Include(m => m.CreatedBy)
            .FirstOrDefaultAsync(m => m.UserId == id);
    }

    public async Task<Manager> AddAsync(Manager manager)
    {
        await _context.Managers.AddAsync(manager);
        await _context.SaveChangesAsync();
        return manager;
    }

    public async Task<Manager> UpdateAsync(Manager manager)
    {
        _context.Managers.Update(manager);
        await _context.SaveChangesAsync();
        return manager;
    }

    public async Task DeleteAsync(long id)
    {
        var manager = await GetByIdAsync(id);
        if (manager != null)
        {
            _context.Managers.Remove(manager);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Manager>> GetByCreatedByAsync(User admin)
    {
        return await _context.Managers
            .Include(m => m.User)
            .Include(m => m.CreatedBy)
            .Where(m => m.CreatedById == admin.Id)
            .ToListAsync();
    }
}