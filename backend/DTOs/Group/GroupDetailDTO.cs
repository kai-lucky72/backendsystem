using System.ComponentModel.DataAnnotations;

namespace backend.DTOs.Group;

public class GroupDetailDTO
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<AgentDTO> Agents { get; set; } = new();
    public AgentDTO? TeamLeader { get; set; }
    public int Performance { get; set; }
    public int CollectedClients { get; set; }
    public string CreatedAt { get; set; } = string.Empty;

    public class AgentDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}