using backend.DTOs.Client;
using backend.Models;

namespace backend.Services;

public interface IClientService
{
    Task<Client> CreateClientAsync(CreateClientRequest request, Agent agent, User currentUser);
    
    Task<Client> GetClientByIdAsync(long id);
    
    Task<Client> GetClientByNationalIdAsync(string nationalId);
    
    Task<Client> GetClientByPhoneNumberAsync(string phoneNumber);
    
    Task<IEnumerable<Client>> GetClientsByAgentAsync(Agent agent);
    
    Task<IEnumerable<Client>> GetClientsByAgentAndDateRangeAsync(Agent agent, DateTime startDate, DateTime endDate);
    
    Task<long> CountClientsByAgentAndDateRangeAsync(Agent agent, DateTime startDate, DateTime endDate);
    
    Task<long> CountClientsByAgentAsync(Agent agent);
    
    Task<IEnumerable<Client>> GetRecentClientsByAgentAsync(Agent agent, int limit);
    
    ClientDTO MapToDTO(Client client);
    
    IEnumerable<ClientDTO> MapToDTOList(IEnumerable<Client> clients);
}