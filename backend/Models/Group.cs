using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("agent_groups")]
public class Group
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Column("manager_id")]
    public long ManagerId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("leader_id")]
    public long? LeaderId { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Manager Manager { get; set; } = null!;
    public virtual Agent? Leader { get; set; }
    public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();
}