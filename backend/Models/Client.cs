using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("clients")]
public class Client
{
    [Key]
    public long Id { get; set; }

    [Required]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Column("national_id")]
    public string NationalId { get; set; } = string.Empty;

    [Required]
    [Column("phone_number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string Location { get; set; } = string.Empty;

    [Required]
    [Column("date_of_birth")]
    public DateOnly DateOfBirth { get; set; }

    [Required]
    [Column("insurance_type")]
    public string InsuranceType { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PayingAmount { get; set; }

    [Required]
    [Column("paying_method")]
    public string PayingMethod { get; set; } = string.Empty;

    [Required]
    [Column("contract_years")]
    public int ContractYears { get; set; }

    [Column("collected_by_name")]
    public string? CollectedByName { get; set; }

    [Column("collected_at")]
    public DateTime? CollectedAt { get; set; }

    [ForeignKey("Agent")]
    [Column("agent_id")]
    public long? AgentId { get; set; }
    
    [ForeignKey("AgentId")]
    public virtual Agent? Agent { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public bool Active { get; set; } = true;
}