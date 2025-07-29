using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace backend.Models;

public enum UserRole
{
    Admin,
    Manager,
    Agent
}

public class User : IdentityUser<long>
{
    [Required]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;
    
    [Column("phone_number")]
    public new string? PhoneNumber { get; set; }
    
    [Column("national_id")]
    public string? NationalId { get; set; }

    [Required]
    [EmailAddress]
    public new string Email { get; set; } = string.Empty;

    [JsonIgnore]
    [Column("password_hash")]
    public new string? PasswordHash { get; set; }

    [Required]
    [Column("work_id")]
    public string WorkId { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    [Column("profile_image_url")]
    public string? ProfileImageUrl { get; set; }
    
    [Column("last_login")]
    public DateTime? LastLogin { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public bool Active { get; set; } = true;

    // Navigation properties
    public virtual Agent? Agent { get; set; }
    public virtual Manager? Manager { get; set; }
    public virtual ICollection<Notification> SentNotifications { get; set; } = new List<Notification>();
    public virtual ICollection<Notification> ReceivedNotifications { get; set; } = new List<Notification>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<Manager> CreatedManagers { get; set; } = new List<Manager>();

    // Override UserName to use WorkId
    public override string UserName
    {
        get => WorkId;
        set => WorkId = value;
    }
}