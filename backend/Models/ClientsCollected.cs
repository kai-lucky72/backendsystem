using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("clients_collected")]
public class ClientsCollected
{
    [Key]
    public long Id { get; set; }

    [Required]
    [ForeignKey("Agent")]
    public long AgentId { get; set; }
    
    [ForeignKey("AgentId")]
    public virtual Agent Agent { get; set; } = null!;

    [Required]
    [Column("collected_at")]
    public DateTime? CollectedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ClientData { get; set; } = string.Empty;
}