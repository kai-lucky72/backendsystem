using System.Net.Http.Json;
using backend.DTOs.External;

namespace backend.Services;

public class ExternalClientService(HttpClient httpClient, IConfiguration configuration, ILogger<ExternalClientService> logger) : IExternalClientService
{
    public async Task<IReadOnlyList<ExternalProposalDto>> GetProposalsByDistributionChannelAsync(string distributionChannelId, CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["ExternalClients:BaseUrl"] ?? "https://apps.prime.rw";
        var path = configuration["ExternalClients:ProposalsPath"] ?? "/customerbackend/api/proposalRegistersByAgent";

        var uri = new Uri(new Uri(baseUrl), path + $"?distributionChannelId={Uri.EscapeDataString(distributionChannelId)}");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.ParseAdd("application/json");
            var res = await httpClient.SendAsync(req, cancellationToken);
            res.EnsureSuccessStatusCode();
            var list = await res.Content.ReadFromJsonAsync<List<ExternalProposalDto>>(cancellationToken: cancellationToken) 
                       ?? new List<ExternalProposalDto>();
            return list;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch external proposals for distributionChannelId {DistributionChannelId}", distributionChannelId);
            throw;
        }
    }
}


