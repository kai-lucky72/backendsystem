using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class GroupRepository : IGroupRepository
{
    private readonly ApplicationDbContext _context;

    public GroupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Group>> GetAllAsync()
    {
        return await _context.Groups
            .Include(g => g.Manager)
            .ThenInclude(m => m!.User)
            .Include(g => g.Leader)
            .ThenInclude(l => l!.User)
            .ToListAsync();
    }

    public async Task<Group?> GetByIdAsync(long id)
    {
        return await _context.Groups
            .Include(g => g.Manager)
            .ThenInclude(m => m!.User)
            .Include(g => g.Leader)
            .ThenInclude(l => l!.User)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Group> AddAsync(Group group)
    {
        await _context.Groups.AddAsync(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task<Group> UpdateAsync(Group group)
    {
        _context.Groups.Update(group);
        await _context.SaveChangesAsync();
        return group;
    }

    public async Task DeleteAsync(long id)
    {
        var group = await GetByIdAsync(id);
        if (group != null)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Group>> GetByManagerAsync(Manager manager)
    {
        return await _context.Groups
            .Include(g => g.Manager)
            .ThenInclude(m => m!.User)
            .Include(g => g.Leader)
            .ThenInclude(l => l!.User)
            .Where(g => g.ManagerId == manager.UserId)
            .ToListAsync();
    }

    public async Task<bool> ExistsByManagerAndNameAsync(Manager manager, string name)
    {
        return await _context.Groups
            .AnyAsync(g => g.ManagerId == manager.UserId && g.Name == name);
    }

    public async Task<Group?> GetByIdWithAgentsAsync(long id)
    {
        return await _context.Groups
            .Include(g => g.Manager)
            .ThenInclude(m => m!.User)
            .Include(g => g.Leader)
            .ThenInclude(l => l!.User)
            .Include(g => g.Agents)
            .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(g => g.Id == id);
    }
}