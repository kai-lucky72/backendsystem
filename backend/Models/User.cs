using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend.Models;

[Table("users")]
public class User
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;
    
    [Column("phone_number")]
    public string? PhoneNumber { get; set; }
    
    [Column("national_id")]
    public string? NationalId { get; set; }

    [Required]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [JsonIgnore]
    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [Column("work_id")]
    public string WorkId { get; set; } = string.Empty;

    [Required]
    [Column("role")]
    public Role Role { get; set; }

    [Column("profile_image_url")]
    public string? ProfileImageUrl { get; set; }
    
    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("active")]
    public bool Active { get; set; } = true;

    // Navigation properties
    public virtual Agent? Agent { get; set; }
    public virtual Manager? Manager { get; set; }
    public virtual ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();
    public virtual ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<Manager> CreatedManagers { get; set; } = new List<Manager>();

    // Computed property for UserName (to maintain compatibility)
    [NotMapped]
    public string UserName => WorkId;
}