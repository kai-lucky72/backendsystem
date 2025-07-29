using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("attendance")]
public class Attendance
{
    [Key]
    public long Id { get; set; }

    [Required]
    [ForeignKey("Agent")]
    [Column("agent_id")]
    public long AgentId { get; set; }
    
    [ForeignKey("AgentId")]
    public virtual Agent Agent { get; set; } = null!;

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    [StringLength(255)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public string Sector { get; set; } = string.Empty;
}