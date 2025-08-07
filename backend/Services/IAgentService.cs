using backend.Models;

namespace backend.Services;

public interface IAgentService
{
    Task<Agent> CreateAgentAsync(string firstName, string lastName, string phoneNumber, string nationalId,
                                string email, string workId, string? password, Manager manager, 
                                AgentType agentType, string sector);
    
    Task<Agent> GetAgentByIdAsync(long id);
    
    Task<IEnumerable<Agent>> GetAllAgentsAsync();
    
    Task<IEnumerable<Agent>> GetAgentsByManagerAsync(Manager manager);
    
    Task<IEnumerable<Agent>> GetAgentsByTypeAsync(AgentType agentType);
    
    Task<IEnumerable<Agent>> GetAgentsByManagerAndTypeAsync(Manager manager, AgentType agentType);
    
    Task<Agent> UpdateAgentSectorAsync(long id, string sector);
    
    Task<Agent> UpdateAgentTypeAsync(long id, AgentType agentType);
    
    Task<IEnumerable<Agent>> GetAgentsByGroupIdAsync(long groupId);

    
    Task DeactivateAgentAsync(long id);
    
    Task<Dictionary<string, object>> GetGroupPerformanceAsync(Group group, DateTime startDateTime, DateTime endDateTime);
    
    Task<IEnumerable<Attendance>> GetAttendanceByAgentAndDateRangeAsync(Agent agent, DateTime startDateTime, DateTime endDateTime);
    
    Task<long> CountClientsByAgentAndDateRangeAsync(Agent agent, DateTime startDateTime, DateTime endDateTime);
    
    Task<Agent> UpdateAgentAsync(long id, Dictionary<string, object> updateFields);
    
    Task DeleteAgentAsync(long id);
}