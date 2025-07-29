using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _context;

    public ClientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Client>> GetAllAsync()
    {
        return await _context.Clients
            .Include(c => c.Agent)
            .ThenInclude(agent => agent!.User)
            .ToListAsync();
    }

    public async Task<Client?> GetByIdAsync(long id)
    {
        return await _context.Clients
            .Include(c => c.Agent)
            .ThenInclude(agent => agent!.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Client> AddAsync(Client client)
    {
        await _context.Clients.AddAsync(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task<Client> UpdateAsync(Client client)
    {
        _context.Clients.Update(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task DeleteAsync(long id)
    {
        var client = await GetByIdAsync(id);
        if (client != null)
        {
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Client>> GetByAgentAsync(Agent agent)
    {
        return await _context.Clients
            .Include(c => c.Agent)
            .ThenInclude(agent => agent!.User)
            .Where(c => c.AgentId == agent.UserId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Client>> GetByAgentAndCreatedAtBetweenAsync(Agent agent, DateTime startDate, DateTime endDate)
    {
        return await _context.Clients
            .Include(c => c.Agent)
            .ThenInclude(agent => agent!.User)
            .Where(c => c.AgentId == agent.UserId && 
                       c.CreatedAt >= startDate && 
                       c.CreatedAt <= endDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Client>> GetByAgentOrderByCreatedAtDescAsync(Agent agent, int page, int pageSize)
    {
        return await _context.Clients
            .Include(c => c.Agent)
            .ThenInclude(agent => agent!.User)
            .Where(c => c.AgentId == agent.UserId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<long> CountByAgentAsync(Agent agent)
    {
        return await _context.Clients
            .CountAsync(c => c.AgentId == agent.UserId);
    }

    public async Task<Client?> GetByNationalIdAsync(string nationalId)
    {
        return await _context.Clients
            .Include(c => c.Agent)
            .ThenInclude(agent => agent!.User)
            .FirstOrDefaultAsync(c => c.NationalId == nationalId);
    }

    public async Task<Client?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Clients
            .Include(c => c.Agent)
            .ThenInclude(agent => agent!.User)
            .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);
    }

    public async Task<bool> ExistsByNationalIdAsync(string nationalId)
    {
        return await _context.Clients
            .AnyAsync(c => c.NationalId == nationalId);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Clients
            .AnyAsync(c => c.PhoneNumber == phoneNumber);
    }

    public async Task<long> CountByAgentAndCreatedAtBetweenAsync(Agent agent, DateTime startDate, DateTime endDate)
    {
        return await _context.Clients
            .CountAsync(c => c.AgentId == agent.UserId && 
                            c.CreatedAt >= startDate && 
                            c.CreatedAt <= endDate);
    }

    public async Task<IEnumerable<Client>> GetByAgentUserIdAsync(long agentId)
    {
        return await _context.Clients
            .Include(c => c.Agent)
            .ThenInclude(agent => agent!.User)
            .Where(c => c.Agent!.UserId == agentId)
            .ToListAsync();
    }
}