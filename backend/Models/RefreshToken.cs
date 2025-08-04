using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace backend.Models;

[Table("refresh_tokens")]
public class RefreshToken
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Column("token")]
    public string Token { get; set; } = string.Empty;

    [Required]
    [Column("user_id")]
    public long UserId { get; set; }

    [Required]
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("revoked_by_ip")]
    public string? RevokedByIp { get; set; }

    [Column("replaced_by_token")]
    public string? ReplacedByToken { get; set; }

    [Column("is_expired")]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    [Column("is_active")]
    public bool IsActive => RevokedAt == null && !IsExpired;

    // Navigation property
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
} 