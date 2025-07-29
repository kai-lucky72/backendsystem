using backend.Models;

namespace backend.Services;

public interface IClientsCollectedService
{
    Task<ClientsCollected> CollectClientAsync(Agent agent, Dictionary<string, object> clientData);
    
    Task<IEnumerable<ClientsCollected>> GetClientsByAgentAsync(Agent agent);
    
    Task<IEnumerable<ClientsCollected>> GetClientsByAgentAndDateRangeAsync(Agent agent, DateTime start, DateTime end);
    
    Task<long> CountClientsByAgentAndDateRangeAsync(Agent agent, DateTime start, DateTime end);
}