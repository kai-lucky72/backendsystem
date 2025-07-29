using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend.Models;

[Table("agents")]
public class Agent
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    [ForeignKey("Manager")]
    [Column("manager_id")]
    public long ManagerId { get; set; }
    
    [ForeignKey("ManagerId")]
    public virtual Manager Manager { get; set; } = null!;

    [Required]
    [Column("agent_type")]
    public AgentTypeEnum AgentType { get; set; }

    [Required]
    public string Sector { get; set; } = string.Empty;

    [ForeignKey("Group")]
    [Column("group_id")]
    public long? GroupId { get; set; }
    
    [ForeignKey("GroupId")]
    [JsonIgnore]
    public virtual Group? Group { get; set; }

    [NotMapped]
    public int ClientsCollected { get; set; }

    public enum AgentTypeEnum
    {
        Sales,
        Individual
    }

    // Navigation properties
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public virtual ICollection<ClientsCollected> ClientsCollectedRecords { get; set; } = new List<ClientsCollected>();
    public virtual ICollection<Group> LedGroups { get; set; } = new List<Group>();
    
    // Utility method to set up the entity correctly
    public static Agent Create(User agentUser, Manager manager, AgentTypeEnum agentType, string sector)
    {
        if (agentUser.Id == 0)
        {
            throw new ArgumentException("Agent user must be persisted with a valid ID");
        }
        
        return new Agent
        {
            UserId = agentUser.Id,
            User = agentUser,
            ManagerId = manager.UserId,
            Manager = manager,
            AgentType = agentType,
            Sector = !string.IsNullOrWhiteSpace(sector) ? sector : "General"
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Agent agent) return false;
        return UserId == agent.UserId;
    }

    public override int GetHashCode()
    {
        return UserId.GetHashCode();
    }
}