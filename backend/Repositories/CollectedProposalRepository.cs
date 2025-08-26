using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class CollectedProposalRepository(ApplicationDbContext db) : ICollectedProposalRepository
{
    public async Task UpsertAsync(long agentId, IEnumerable<CollectedProposal> proposals, CancellationToken cancellationToken = default)
    {
        var incomingByNumber = proposals.ToDictionary(p => p.ProposalNumber, StringComparer.OrdinalIgnoreCase);
        var numbers = incomingByNumber.Keys.ToList();

        var existing = await db.CollectedProposals
            .Where(p => numbers.Contains(p.ProposalNumber))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        // Update existing
        foreach (var e in existing)
        {
            var src = incomingByNumber[e.ProposalNumber];
            e.AgentId = agentId;
            e.CustomerCode = src.CustomerCode;
            e.CustomerName = src.CustomerName;
            e.ProposalDate = src.ProposalDate;
            e.Premium = src.Premium;
            e.RiskPremium = src.RiskPremium;
            e.SavingsPremium = src.SavingsPremium;
            e.TotalPremium = src.TotalPremium;
            e.PremiumFrequency = src.PremiumFrequency;
            e.PaymentMode = src.PaymentMode;
            e.Institutions = src.Institutions;
            e.DueDate = src.DueDate;
            e.Converted = src.Converted;
            e.ConvertedDate = src.ConvertedDate;
            e.FetchedAtUtc = now;
        }

        // Insert new
        var existingSet = new HashSet<string>(existing.Select(x => x.ProposalNumber), StringComparer.OrdinalIgnoreCase);
        foreach (var kv in incomingByNumber)
        {
            if (existingSet.Contains(kv.Key)) continue;
            var s = kv.Value;
            db.CollectedProposals.Add(new CollectedProposal
            {
                AgentId = agentId,
                ProposalNumber = s.ProposalNumber ?? string.Empty,
                CustomerCode = s.CustomerCode,
                CustomerName = s.CustomerName,
                ProposalDate = s.ProposalDate,
                Premium = s.Premium,
                RiskPremium = s.RiskPremium,
                SavingsPremium = s.SavingsPremium,
                TotalPremium = s.TotalPremium,
                PremiumFrequency = s.PremiumFrequency,
                PaymentMode = s.PaymentMode,
                Institutions = s.Institutions,
                DueDate = s.DueDate,
                Converted = s.Converted,
                ConvertedDate = s.ConvertedDate,
                FetchedAtUtc = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CollectedProposal>> GetByAgentAndDateRangeAsync(long agentId, DateTime fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var q = db.CollectedProposals.AsNoTracking().Where(p => p.AgentId == agentId && p.ProposalDate >= fromDate);
        if (toDate.HasValue)
        {
            q = q.Where(p => p.ProposalDate <= toDate.Value);
        }
        return await q.OrderByDescending(p => p.ProposalDate).ToListAsync(cancellationToken);
    }
}


