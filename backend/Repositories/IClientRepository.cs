using backend.Models;

namespace backend.Repositories;

public interface IClientRepository
{
    Task<IEnumerable<Client>> GetAllAsync();
    Task<Client?> GetByIdAsync(long id);
    Task<Client> AddAsync(Client client);
    Task<Client> UpdateAsync(Client client);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<IEnumerable<Client>> GetByAgentAsync(Agent agent);
    Task<IEnumerable<Client>> GetByAgentAndCreatedAtBetweenAsync(Agent agent, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Client>> GetByAgentOrderByCreatedAtDescAsync(Agent agent, int page, int pageSize);
    Task<long> CountByAgentAsync(Agent agent);
    Task<Client?> GetByNationalIdAsync(string nationalId);
    Task<Client?> GetByPhoneNumberAsync(string phoneNumber);
    Task<bool> ExistsByNationalIdAsync(string nationalId);
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber);
    Task<long> CountByAgentAndCreatedAtBetweenAsync(Agent agent, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Client>> GetByAgentUserIdAsync(long agentId);
}