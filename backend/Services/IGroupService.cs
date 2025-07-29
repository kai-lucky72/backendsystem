using backend.Models;

namespace backend.Services;

public interface IGroupService
{
    Task<Group> CreateGroupAsync(string name, Manager manager);
    
    Task<Group> CreateGroupWithDescriptionAsync(string name, string description, Manager manager);
    
    Task<Group> SaveGroupAsync(Group group);
    
    Task<Group> GetGroupByIdAsync(long id);
    
    Task<IEnumerable<Group>> GetGroupsByManagerAsync(Manager manager);
    
    Task<Group> UpdateGroupNameAsync(long id, string name);
    
    Task<Group> AssignLeaderAsync(long groupId, long agentId);
    
    Task<Group> AddAgentToGroupAsync(long groupId, long agentId);
    
    Task<Group> RemoveAgentFromGroupAsync(long groupId, long agentId);
    
    Task<IEnumerable<Agent>> GetGroupAgentsAsync(long groupId);
    
    Task DeleteGroupAsync(long id);
}