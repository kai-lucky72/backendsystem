using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class ClientsCollectedRepository : IClientsCollectedRepository
{
    private readonly ApplicationDbContext _context;

    public ClientsCollectedRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ClientsCollected>> GetAllAsync()
    {
        return await _context.ClientsCollected
            .Include(cc => cc.Agent)
            .ThenInclude(agent => agent!.User)
            .ToListAsync();
    }

    public async Task<ClientsCollected?> GetByIdAsync(long id)
    {
        return await _context.ClientsCollected
            .Include(cc => cc.Agent)
            .ThenInclude(agent => agent!.User)
            .FirstOrDefaultAsync(cc => cc.Id == id);
    }

    public async Task<ClientsCollected> AddAsync(ClientsCollected clientsCollected)
    {
        await _context.ClientsCollected.AddAsync(clientsCollected);
        await _context.SaveChangesAsync();
        return clientsCollected;
    }

    public async Task<ClientsCollected> UpdateAsync(ClientsCollected clientsCollected)
    {
        _context.ClientsCollected.Update(clientsCollected);
        await _context.SaveChangesAsync();
        return clientsCollected;
    }

    public async Task DeleteAsync(long id)
    {
        var clientsCollected = await GetByIdAsync(id);
        if (clientsCollected != null)
        {
            _context.ClientsCollected.Remove(clientsCollected);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ClientsCollected>> GetByAgentOrderByCollectedAtDescAsync(Agent agent)
    {
        return await _context.ClientsCollected
            .Include(cc => cc.Agent)
            .ThenInclude(agent => agent!.User)
            .Where(cc => cc.AgentId == agent.UserId)
            .OrderByDescending(cc => cc.CollectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ClientsCollected>> GetByAgentAndDateRangeAsync(Agent agent, DateTime startDate, DateTime endDate)
    {
        return await _context.ClientsCollected
            .Include(cc => cc.Agent)
            .ThenInclude(agent => agent!.User)
            .Where(cc => cc.AgentId == agent.UserId && 
                        cc.CollectedAt >= startDate && 
                        cc.CollectedAt <= endDate)
            .OrderByDescending(cc => cc.CollectedAt)
            .ToListAsync();
    }

    public async Task<long> CountByAgentAndDateRangeAsync(Agent agent, DateTime startDate, DateTime endDate)
    {
        return await _context.ClientsCollected
            .CountAsync(cc => cc.AgentId == agent.UserId && 
                             cc.CollectedAt >= startDate && 
                             cc.CollectedAt <= endDate);
    }
}