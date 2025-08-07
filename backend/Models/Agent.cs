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

    [Required]
    [Column("manager_id")]
    public long ManagerId { get; set; }

    [Column("group_id")]
    public long? GroupId { get; set; }

    [Required]
    [Column("agent_type")]
    public AgentType AgentType { get; set; }

    [Required]
    public string Sector { get; set; } = string.Empty;

    // Navigation properties
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
    [JsonIgnore]
    public virtual Manager Manager { get; set; } = null!;
    [JsonIgnore]
    public virtual Group? Group { get; set; }
    [JsonIgnore]
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
    [JsonIgnore]
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    [JsonIgnore]
    public virtual ICollection<ClientsCollected> ClientsCollectedRecords { get; set; } = new List<ClientsCollected>();
    [JsonIgnore]
    public virtual ICollection<Group> LedGroups { get; set; } = new List<Group>();

    // Computed properties
    [NotMapped]
    public int ClientsCollected { get; set; }

    // Utility method to set up the entity correctly
    public static Agent Create(User agentUser, Manager manager, AgentType agentType, string sector)
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
}