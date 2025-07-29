using backend.Models;

namespace backend.Repositories;

public interface IAgentRepository
{
    Task<IEnumerable<Agent>> GetAllAsync();
    Task<Agent?> GetByIdAsync(long id);
    Task<Agent> AddAsync(Agent agent);
    Task<Agent> UpdateAsync(Agent agent);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<IEnumerable<Agent>> GetByManagerAsync(Manager manager);
    Task<IEnumerable<Agent>> GetByGroupAsync(Group group);
    Task<IEnumerable<Agent>> GetByManagerAndAgentTypeAsync(Manager manager, Agent.AgentTypeEnum agentType);
    Task<Agent?> GetByUserIdAsync(long userId);
}