using backend.Models;

namespace backend.Repositories;

public interface IClientsCollectedRepository
{
    Task<IEnumerable<ClientsCollected>> GetAllAsync();
    Task<ClientsCollected?> GetByIdAsync(long id);
    Task<ClientsCollected> AddAsync(ClientsCollected clientsCollected);
    Task<ClientsCollected> UpdateAsync(ClientsCollected clientsCollected);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<IEnumerable<ClientsCollected>> GetByAgentOrderByCollectedAtDescAsync(Agent agent);
    Task<IEnumerable<ClientsCollected>> GetByAgentAndDateRangeAsync(Agent agent, DateTime startDate, DateTime endDate);
    Task<long> CountByAgentAndDateRangeAsync(Agent agent, DateTime startDate, DateTime endDate);
}