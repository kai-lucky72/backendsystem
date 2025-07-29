using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("managers")]
public class Manager
{
    [Key]
    [Column("user_id")]
    public long UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [Required]
    [ForeignKey("CreatedBy")]
    [Column("created_by")]
    public long CreatedById { get; set; }
    
    [ForeignKey("CreatedById")]
    public virtual User CreatedBy { get; set; } = null!;
    
    [Column("department")]
    public string? Department { get; set; }

    // Navigation properties
    public virtual ICollection<Agent> Agents { get; set; } = new List<Agent>();
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
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