using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

[Table("collected_proposals")]
public class CollectedProposal
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Required]
    [Column("agent_id")]
    public long AgentId { get; set; }

    [Required]
    [Column("proposal_number")]
    public string ProposalNumber { get; set; } = string.Empty;

    [Column("customer_code")]
    public string? CustomerCode { get; set; }

    [Column("customer_name")]
    public string? CustomerName { get; set; }

    [Column("proposal_date")]
    public DateTime? ProposalDate { get; set; }

    [Column("premium")]
    public decimal? Premium { get; set; }

    [Column("risk_premium")]
    public decimal? RiskPremium { get; set; }

    [Column("savings_premium")]
    public decimal? SavingsPremium { get; set; }

    [Column("total_premium")]
    public decimal? TotalPremium { get; set; }

    [Column("premium_frequency")]
    public string? PremiumFrequency { get; set; }

    [Column("payment_mode")]
    public string? PaymentMode { get; set; }

    [Column("institutions")]
    public string? Institutions { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("converted")]
    public bool? Converted { get; set; }

    [Column("converted_date")]
    public DateTime? ConvertedDate { get; set; }

    [Column("fetched_at_utc")]
    public DateTime FetchedAtUtc { get; set; }
}


