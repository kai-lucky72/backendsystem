using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace backend.Services;

public class ClientsCollectedService : IClientsCollectedService
{
    private readonly ILogger<ClientsCollectedService> _logger;
    private readonly IClientsCollectedRepository _clientsCollectedRepository;
    private readonly IAttendanceService _attendanceService;
    private readonly IAuditLogService _auditLogService;

    public ClientsCollectedService(
        ILogger<ClientsCollectedService> logger,
        IClientsCollectedRepository clientsCollectedRepository,
        IAttendanceService attendanceService,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _clientsCollectedRepository = clientsCollectedRepository;
        _attendanceService = attendanceService;
        _auditLogService = auditLogService;
    }

    public async Task<ClientsCollected> CollectClientAsync(Agent agent, Dictionary<string, object> clientData)
    {
        // Check if agent has marked attendance today
        if (!await _attendanceService.HasMarkedAttendanceTodayAsync(agent))
        {
            throw new InvalidOperationException("Agent must mark attendance before collecting clients");
        }
        
        // Validate required client data
        ValidateClientData(clientData);
        
        // Serialize clientData to JSON string
        string clientDataJson;
        try
        {
            clientDataJson = JsonSerializer.Serialize(clientData);
        }
        catch (JsonException e)
        {
            throw new InvalidOperationException("Failed to serialize client data to JSON", e);
        }
        
        // Create and save client collection
        var clientsCollected = new ClientsCollected
        {
            Agent = agent,
            CollectedAt = DateTime.Now,
            ClientData = clientDataJson
        };
        
        var savedClient = await _clientsCollectedRepository.AddAsync(clientsCollected);
        
        // Log the action
        await _auditLogService.LogEventAsync(
                "COLLECT_CLIENT",
                "CLIENT",
                savedClient.Id.ToString(),
                agent.User,
                $"Client collected at {savedClient.CollectedAt}"
        );
        
        return savedClient;
    }

    public async Task<IEnumerable<ClientsCollected>> GetClientsByAgentAsync(Agent agent)
    {
        return await _clientsCollectedRepository.GetByAgentAsync(agent);
    }

    public async Task<IEnumerable<ClientsCollected>> GetClientsByAgentAndDateRangeAsync(Agent agent, DateTime start, DateTime end)
    {
        return await _clientsCollectedRepository.GetByAgentAndDateRangeAsync(agent, start, end);
    }

    public async Task<long> CountClientsByAgentAndDateRangeAsync(Agent agent, DateTime start, DateTime end)
    {
        return await _clientsCollectedRepository.CountByAgentAndDateRangeAsync(agent, start, end);
    }
    
    private void ValidateClientData(Dictionary<string, object> clientData)
    {
        if (clientData == null || !clientData.Any())
        {
            throw new InvalidOperationException("Client data cannot be empty");
        }
        
        // Check for required fields
        var requiredFields = new[] { "name", "contact", "sector" };
        
        foreach (var field in requiredFields)
        {
            if (!clientData.ContainsKey(field) || clientData[field] == null || 
                    clientData[field].ToString()?.Trim().Length == 0)
            {
                throw new InvalidOperationException($"Required field missing or empty: {field}");
            }
        }
    }
}