using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly ApplicationDbContext _context;

    public AgentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Agent>> GetAllAsync()
    {
        return await _context.Agents
            .Include(a => a.User)
            .Include(a => a.Manager)
            .Include(a => a.Group)
            .ToListAsync();
    }

    public async Task<Agent?> GetByIdAsync(long id)
    {
        return await _context.Agents
            .Include(a => a.User)
            .Include(a => a.Manager)
            .Include(a => a.Group)
            .FirstOrDefaultAsync(a => a.UserId == id);
    }

    public async Task<Agent> AddAsync(Agent agent)
    {
        await _context.Agents.AddAsync(agent);
        await _context.SaveChangesAsync();
        return agent;
    }

    public async Task<Agent> UpdateAsync(Agent agent)
    {
        _context.Agents.Update(agent);
        await _context.SaveChangesAsync();
        return agent;
    }

    public async Task DeleteAsync(long id)
    {
        var agent = await GetByIdAsync(id);
        if (agent != null)
        {
            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Agent>> GetByManagerAsync(Manager manager)
    {
        return await _context.Agents
            .Include(a => a.User)
            .Include(a => a.Manager)
            .Include(a => a.Group)
            .Where(a => a.ManagerId == manager.UserId || a.ManagerId == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<Agent>> GetByGroupAsync(Group group)
    {
        return await _context.Agents
            .Include(a => a.User)
            .Include(a => a.Manager)
            .Include(a => a.Group)
            .Where(a => a.GroupId == group.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<Agent>> GetByManagerAndAgentTypeAsync(Manager manager, AgentType agentType)
    {
        return await _context.Agents
            .Include(a => a.User)
            .Include(a => a.Manager)
            .Include(a => a.Group)
            .Where(a => a.ManagerId == manager.UserId && a.AgentType == agentType)
            .ToListAsync();
    }

    public async Task<Agent?> GetByUserIdAsync(long userId)
    {
        return await _context.Agents
            .Include(a => a.User)
            .Include(a => a.Manager)
            .Include(a => a.Group)
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }
}