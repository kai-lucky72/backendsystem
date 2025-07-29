using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class GroupService : IGroupService
{
    private readonly ILogger<GroupService> _logger;
    private readonly IGroupRepository _groupRepository;
    private readonly IAgentService _agentService;
    private readonly IAuditLogService _auditLogService;

    public GroupService(
        ILogger<GroupService> logger,
        IGroupRepository groupRepository,
        IAgentService agentService,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _groupRepository = groupRepository;
        _agentService = agentService;
        _auditLogService = auditLogService;
    }

    public async Task<Group> CreateGroupAsync(string name, Manager manager)
    {
        if (await _groupRepository.ExistsByManagerAndNameAsync(manager, name))
        {
            throw new InvalidOperationException($"Group with name '{name}' already exists for this manager");
        }
        
        var group = new Group
        {
            Name = name,
            Manager = manager
        };
        
        var savedGroup = await _groupRepository.AddAsync(group);
        
        await _auditLogService.LogEventAsync(
                "CREATE_GROUP",
                "GROUP",
                savedGroup.Id.ToString(),
                manager.User,
                $"Group created: {name}"
        );
        
        return savedGroup;
    }

    public async Task<Group> CreateGroupWithDescriptionAsync(string name, string description, Manager manager)
    {
        if (await _groupRepository.ExistsByManagerAndNameAsync(manager, name))
        {
            throw new InvalidOperationException($"Group with name '{name}' already exists for this manager");
        }
        
        var group = new Group
        {
            Name = name,
            Description = description,
            Manager = manager
        };
        
        var savedGroup = await _groupRepository.AddAsync(group);
        
        await _auditLogService.LogEventAsync(
                "CREATE_GROUP",
                "GROUP",
                savedGroup.Id.ToString(),
                manager.User,
                $"Group created: {name}"
        );
        
        return savedGroup;
    }

    public async Task<Group> SaveGroupAsync(Group group)
    {
        return await _groupRepository.AddAsync(group);
    }

    public async Task<Group> GetGroupByIdAsync(long id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null)
        {
            throw new InvalidOperationException($"Group not found with ID: {id}");
        }
        return group;
    }

    public async Task<IEnumerable<Group>> GetGroupsByManagerAsync(Manager manager)
    {
        return await _groupRepository.GetByManagerAsync(manager);
    }

    public async Task<Group> UpdateGroupNameAsync(long id, string name)
    {
        var group = await GetGroupByIdAsync(id);
        
        if (await _groupRepository.ExistsByManagerAndNameAsync(group.Manager, name))
        {
            throw new InvalidOperationException($"Group with name '{name}' already exists for this manager");
        }
        
        group.Name = name;
        var updatedGroup = await _groupRepository.UpdateAsync(group);
        
        await _auditLogService.LogEventAsync(
                "UPDATE_GROUP",
                "GROUP",
                id.ToString(),
                group.Manager.User,
                $"Group name updated: {name}"
        );
        
        return updatedGroup;
    }

    public async Task<Group> AssignLeaderAsync(long groupId, long agentId)
    {
        var group = await GetGroupByIdAsync(groupId);
        var agent = await _agentService.GetAgentByIdAsync(agentId);
        
        // Verify agent belongs to this group
        if (!group.Agents.Contains(agent))
        {
            throw new InvalidOperationException("Agent must be a member of the group to be assigned as leader");
        }
        
        // Verify agent is a SALES agent
        if (agent.AgentType != Agent.AgentTypeEnum.Sales)
        {
            throw new InvalidOperationException("Only sales agents can be assigned as group leaders");
        }
        
        group.Leader = agent;
        var updatedGroup = await _groupRepository.UpdateAsync(group);
        
        await _auditLogService.LogEventAsync(
                "ASSIGN_LEADER",
                "GROUP",
                groupId.ToString(),
                group.Manager.User,
                $"Leader assigned: Agent ID {agentId}"
        );
        
        return updatedGroup;
    }

    public async Task<Group> AddAgentToGroupAsync(long groupId, long agentId)
    {
        var group = await GetGroupByIdAsync(groupId);
        var agent = await _agentService.GetAgentByIdAsync(agentId);
        
        // Verify agent belongs to the same manager as the group
        if (agent.Manager.UserId != group.Manager.UserId)
        {
            throw new InvalidOperationException("Agent and group must belong to the same manager");
        }
        
        // Verify agent is a SALES agent
        if (agent.AgentType != Agent.AgentTypeEnum.Sales)
        {
            throw new InvalidOperationException("Only sales agents can be added to groups");
        }
        
        // Prevent agent from being in two groups at the same time
        if (agent.Group != null && agent.Group.Id != group.Id)
        {
            throw new InvalidOperationException("Agent is already assigned to another group");
        }
        
        group.Agents.Add(agent);
        agent.Group = group; // Ensure the agent's group is set
        
        var updateFields = new Dictionary<string, object> { ["group"] = group };
        await _agentService.UpdateAgentAsync(agent.UserId, updateFields); // Save the agent with the new group
        
        var updatedGroup = await _groupRepository.UpdateAsync(group);
        
        await _auditLogService.LogEventAsync(
                "ADD_AGENT_TO_GROUP",
                "GROUP",
                groupId.ToString(),
                group.Manager.User,
                $"Agent added: Agent ID {agentId}"
        );
        
        return updatedGroup;
    }

    public async Task<Group> RemoveAgentFromGroupAsync(long groupId, long agentId)
    {
        try
        {
            _logger.LogInformation("Attempting to remove agent {AgentId} from group {GroupId}", agentId, groupId);
            
            var group = await GetGroupByIdAsync(groupId);
            var agent = await _agentService.GetAgentByIdAsync(agentId);
            
            _logger.LogInformation("Group agents before: {AgentIds}", 
                string.Join(", ", group.Agents.Select(a => a.UserId)));
            _logger.LogInformation("Agent's current group: {GroupId}", 
                agent.Group?.Id);
            
            if (agent.Group == null || groupId != agent.Group.Id)
            {
                _logger.LogWarning("Agent {AgentId} is not a member of group {GroupId}", agentId, groupId);
                throw new InvalidOperationException("Agent is not a member of this group");
            }
            
            group.Agents.Remove(agent);
            _logger.LogInformation("Group agents after removal: {AgentIds}", 
                string.Join(", ", group.Agents.Select(a => a.UserId)));
            
            if (group.Leader != null && group.Leader.UserId == agentId)
            {
                group.Leader = null;
                _logger.LogInformation("Agent {AgentId} was the leader and has been removed as leader", agentId);
            }
            
            agent.Group = null;
            var updateFields = new Dictionary<string, object> { ["group"] = null };
            await _agentService.UpdateAgentAsync(agent.UserId, updateFields);
            _logger.LogInformation("Agent {AgentId} group set to null and updated", agentId);
            
            var updatedGroup = await _groupRepository.UpdateAsync(group);
            _logger.LogInformation("Group {GroupId} saved after agent removal", groupId);
            
            await _auditLogService.LogEventAsync(
                    "REMOVE_AGENT_FROM_GROUP",
                    "GROUP",
                    groupId.ToString(),
                    group.Manager.User,
                    $"Agent removed: Agent ID {agentId}"
            );
            
            return updatedGroup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing agent {AgentId} from group {GroupId}", agentId, groupId);
            throw;
        }
    }

    public async Task<IEnumerable<Agent>> GetGroupAgentsAsync(long groupId)
    {
        var group = await GetGroupByIdAsync(groupId);
        return group.Agents;
    }

    public async Task DeleteGroupAsync(long id)
    {
        var group = await GetGroupByIdAsync(id);
        
        // Unassign all agents from this group
        foreach (var agent in group.Agents)
        {
            agent.Group = null;
            var updateFields = new Dictionary<string, object> { ["group"] = null };
            await _agentService.UpdateAgentAsync(agent.UserId, updateFields);
        }
        
        group.Agents.Clear();
        group.Leader = null;
        await _groupRepository.UpdateAsync(group);
        
        // Then delete the group
        await _groupRepository.DeleteAsync(id);
        
        await _auditLogService.LogEventAsync(
                "DELETE_GROUP",
                "GROUP",
                id.ToString(),
                group.Manager.User,
                $"Group deleted: {group.Name}"
        );
    }
}