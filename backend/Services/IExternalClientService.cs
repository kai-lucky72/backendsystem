using backend.DTOs.External;

namespace backend.Services;

public interface IExternalClientService
{
    Task<IReadOnlyList<ExternalProposalDto>> GetProposalsByDistributionChannelAsync(string distributionChannelId, CancellationToken cancellationToken = default);
}


