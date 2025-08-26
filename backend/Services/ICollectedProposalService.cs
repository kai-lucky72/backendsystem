using backend.DTOs.External;
using backend.Models;

namespace backend.Services;

public interface ICollectedProposalService
{
    Task<(int inserted, int updated)> SyncAgentProposalsAsync(long agentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CollectedProposal>> GetAgentProposalsAsync(long agentId, DateTime fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
}


