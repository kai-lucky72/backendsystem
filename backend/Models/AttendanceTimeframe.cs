using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("attendance_timeframes")]
public class AttendanceTimeframe
{
    [Key]
    public long Id { get; set; }
    
    [Column("manager_id")]
    public long ManagerId { get; set; }

    [ForeignKey("ManagerId")]
    public virtual Manager? Manager { get; set; }
    
    [Column("day_of_week")]
    public byte DayOfWeek { get; set; }
    
    [Required]
    [Column("start_time")]
    public TimeOnly StartTime { get; set; }

    [Required]
    [Column("end_time")]
    public TimeOnly EndTime { get; set; }
    
    [Required]
    [Column("break_duration")]
    public int BreakDuration { get; set; } = 60;
    
    [Required]
    [Column("applies_to_all_agents")]
    public bool AppliesToAllAgents { get; set; } = true;

    [Required]
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}