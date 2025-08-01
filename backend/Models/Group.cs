using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend.Models;

[Table("agent_groups")]
public class Group
{
    [Key]
    public long Id { get; set; }

    [Required]
    [ForeignKey("Manager")]
    [Column("manager_id")]
    public long ManagerId { get; set; }
    
    [ForeignKey("ManagerId")]
    public virtual Manager Manager { get; set; } = null!;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("Leader")]
    [Column("leader_id")]
    public long? LeaderId { get; set; }
    
    [ForeignKey("LeaderId")]
    public virtual Agent? Leader { get; set; }

    [JsonIgnore]
    public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();

    public ICollection<Agent> GetAgents()
    {
        return Agents ?? new List<Agent>();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Group group) return false;
        return Id == group.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}