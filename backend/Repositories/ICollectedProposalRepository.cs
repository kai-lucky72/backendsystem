using backend.Models;

namespace backend.Repositories;

public interface ICollectedProposalRepository
{
    Task UpsertAsync(long agentId, IEnumerable<CollectedProposal> proposals, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CollectedProposal>> GetByAgentAndDateRangeAsync(long agentId, DateTime fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
}


