using backend.DTOs.External;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class CollectedProposalService(
    IExternalClientService externalClientService,
    IAgentService agentService,
    ICollectedProposalRepository collectedProposalRepository,
    ILogger<CollectedProposalService> logger)
    : ICollectedProposalService
{
    public async Task<(int inserted, int updated)> SyncAgentProposalsAsync(long agentId, CancellationToken cancellationToken = default)
    {
        var agent = await agentService.GetAgentByIdAsync(agentId) ?? throw new InvalidOperationException($"Agent not found: {agentId}");
        var extCode = agent.ExternalDistributionChannelId;
        if (string.IsNullOrWhiteSpace(extCode))
        {
            throw new InvalidOperationException("Agent has no ExternalDistributionChannelId set.");
        }

        var proposals = await externalClientService.GetProposalsByDistributionChannelAsync(extCode!, cancellationToken);

        var anchorDate = agent.User.CreatedAt.Date;
        var filtered = proposals.Where(p => p.ProposalDate == null || p.ProposalDate.Value.Date >= anchorDate).ToList();

        // Map for upsert
        var mapped = filtered
            .Where(p => !string.IsNullOrWhiteSpace(p.ProposalNumber))
            .Select(p => new CollectedProposal
            {
                AgentId = agentId,
                ProposalNumber = p.ProposalNumber!,
                CustomerCode = p.CustomerCode,
                CustomerName = p.CustomerName,
                ProposalDate = p.ProposalDate,
                Premium = p.Premium,
                RiskPremium = p.RiskPremium,
                SavingsPremium = p.SavingsPremium,
                TotalPremium = p.TotalPremium,
                PremiumFrequency = p.PremiumFrequency,
                PaymentMode = p.PaymentMode,
                Institutions = p.Institutions,
                DueDate = p.DueDate,
                Converted = p.Converted,
                ConvertedDate = p.ConvertedDate,
                FetchedAtUtc = DateTime.UtcNow
            });

        // To compute inserted/updated, we need to know existing set
        // Simpler approach: fetch existing by numbers
        var numbers = mapped.Select(m => m.ProposalNumber).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        int beforeExistingCount = 0;
        try { /* Optional: could query repo for counts; skip for performance */ }
        catch { }

        await collectedProposalRepository.UpsertAsync(agentId, mapped, cancellationToken);

        // We cannot easily know inserted vs updated without extra queries; return -1/-1 as placeholder or compute properly if needed.
        return (-1, -1);
    }

    public Task<IReadOnlyList<CollectedProposal>> GetAgentProposalsAsync(long agentId, DateTime fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
        => collectedProposalRepository.GetByAgentAndDateRangeAsync(agentId, fromDate, toDate, cancellationToken);
}


