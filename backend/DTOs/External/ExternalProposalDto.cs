namespace backend.DTOs.External;

public class ExternalProposalDto
{
    public string? ProposalNumber { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }
    public DateTime? ProposalDate { get; set; }
    public decimal? Premium { get; set; }
    public decimal? RiskPremium { get; set; }
    public decimal? SavingsPremium { get; set; }
    public decimal? TotalPremium { get; set; }
    public string? PremiumFrequency { get; set; }
    public string? PaymentMode { get; set; }
    public string? Institutions { get; set; }
    public DateTime? DueDate { get; set; }
    public bool? Converted { get; set; }
    public DateTime? ConvertedDate { get; set; }
}


