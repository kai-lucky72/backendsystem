using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend.Models;

[Table("managers")]
public class Manager
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [ForeignKey("UserId")]
    [JsonIgnore]
    public virtual User User { get; set; } = null!;

    [Required]
    [ForeignKey("CreatedBy")]
    [Column("created_by")]
    public long CreatedById { get; set; }
    
    [ForeignKey("CreatedById")]
    [JsonIgnore]
    public virtual User CreatedBy { get; set; } = null!;
    
    [Column("department")]
    public string? Department { get; set; }

    // Navigation properties
    [JsonIgnore]
    public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();
    [JsonIgnore]
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    [JsonIgnore]
    public virtual ICollection<AttendanceTimeframe> AttendanceTimeframes { get; set; } = new List<AttendanceTimeframe>();
    
    // Utility method to set up the entity correctly
    public static Manager Create(User managerUser, User admin)
    {
        if (managerUser.Id == 0)
        {
            throw new ArgumentException("Manager user must be persisted with a valid ID");
        }
        
        return new Manager
        {
            UserId = managerUser.Id,
            User = managerUser,
            CreatedById = admin.Id,
            CreatedBy = admin
        };
    }
}