using backend.DTOs.Client;
using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class ClientService : IClientService
{
    private readonly ILogger<ClientService> _logger;
    private readonly IClientRepository _clientRepository;
    private readonly IAuditLogService _auditLogService;

    public ClientService(
        ILogger<ClientService> logger,
        IClientRepository clientRepository,
        IAuditLogService auditLogService)
    {
        _logger = logger;
        _clientRepository = clientRepository;
        _auditLogService = auditLogService;
    }

    public async Task<Client> CreateClientAsync(CreateClientRequest request, Agent agent, User currentUser)
    {
        // Check if client with same national ID or phone number already exists
        if (await _clientRepository.ExistsByNationalIdAsync(request.NationalId))
        {
            throw new ArgumentException("Client with this National ID already exists");
        }
        
        if (await _clientRepository.ExistsByPhoneNumberAsync(request.PhoneNumber))
        {
            throw new ArgumentException("Client with this Phone Number already exists");
        }
        
        var client = new Client
        {
            FullName = request.FullName,
            NationalId = request.NationalId,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Location = request.Location,
            DateOfBirth = request.DateOfBirth,
            InsuranceType = request.InsuranceType,
            PayingAmount = request.PayingAmount,
            PayingMethod = request.PayingMethod,
            ContractYears = request.ContractYears,
            Agent = agent,
            Active = true,
            CollectedByName = $"{agent.User.FirstName} {agent.User.LastName}",
            CollectedAt = DateTime.Now
        };

        var savedClient = await _clientRepository.AddAsync(client);
        
        // Log the action
        await _auditLogService.LogEventAsync(
                "CREATE_CLIENT",
                "CLIENT",
                savedClient.Id.ToString(),
                currentUser,
                $"Client created: {request.FullName}, Insurance Type: {request.InsuranceType}"
        );
        
        return savedClient;
    }

    public async Task<Client> GetClientByIdAsync(long id)
    {
        var client = await _clientRepository.GetByIdAsync(id);
        if (client == null)
        {
            throw new InvalidOperationException($"Client not found with ID: {id}");
        }
        return client;
    }

    public async Task<Client> GetClientByNationalIdAsync(string nationalId)
    {
        var client = await _clientRepository.GetByNationalIdAsync(nationalId);
        if (client == null)
        {
            throw new InvalidOperationException($"Client not found with National ID: {nationalId}");
        }
        return client;
    }

    public async Task<Client> GetClientByPhoneNumberAsync(string phoneNumber)
    {
        var client = await _clientRepository.GetByPhoneNumberAsync(phoneNumber);
        if (client == null)
        {
            throw new InvalidOperationException($"Client not found with Phone Number: {phoneNumber}");
        }
        return client;
    }

    public async Task<IEnumerable<Client>> GetClientsByAgentAsync(Agent agent)
    {
        return await _clientRepository.GetByAgentAsync(agent);
    }

    public async Task<IEnumerable<Client>> GetClientsByAgentAndDateRangeAsync(Agent agent, DateTime startDate, DateTime endDate)
    {
        return await _clientRepository.GetByAgentAndDateRangeAsync(agent, startDate, endDate);
    }

    public async Task<long> CountClientsByAgentAndDateRangeAsync(Agent agent, DateTime startDate, DateTime endDate)
    {
        return await _clientRepository.CountByAgentAndDateRangeAsync(agent, startDate, endDate);
    }
    
    public async Task<long> CountClientsByAgentAsync(Agent agent)
    {
        return await _clientRepository.CountByAgentAsync(agent);
    }
    
    public async Task<IEnumerable<Client>> GetRecentClientsByAgentAsync(Agent agent, int limit)
    {
        var allClients = await _clientRepository.GetByAgentAsync(agent);
        return allClients.OrderByDescending(c => c.CreatedAt).Take(limit);
    }

    public ClientDTO MapToDTO(Client client)
    {
        return new ClientDTO
        {
            Id = $"cli-{client.Id:D3}",
            FullName = client.FullName,
            NationalId = client.NationalId,
            PhoneNumber = client.PhoneNumber,
            Email = client.Email,
            Location = client.Location,
            DateOfBirth = client.DateOfBirth,
            InsuranceType = client.InsuranceType,
            PayingAmount = client.PayingAmount,
            PayingMethod = client.PayingMethod,
            ContractYears = client.ContractYears,
            AgentId = $"agt-{client.Agent.UserId:D3}",
            AgentName = $"{client.Agent.User.FirstName} {client.Agent.User.LastName}",
            CreatedAt = client.CreatedAt,
            Active = client.Active
        };
    }

    public IEnumerable<ClientDTO> MapToDTOList(IEnumerable<Client> clients)
    {
        return clients.Select(MapToDTO);
    }
}