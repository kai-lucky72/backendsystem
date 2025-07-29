using backend.Models;

namespace backend.Services;

public interface IManagerService
{
    Task<Manager> CreateManagerAsync(string firstName, string lastName, string phoneNumber, string nationalId, 
                                    string email, string workId, string? password, User admin);
    
    Task<Manager> GetManagerByIdAsync(long id);
    
    Task<IEnumerable<Manager>> GetAllManagersAsync();
    
    Task<IEnumerable<Manager>> GetManagersByAdminAsync(User admin);
    
    Task DeleteManagerAsync(long id);

    Task<Manager> UpdateManagerAsync(long id, Dictionary<string, object> updateFields);

    Task<Dictionary<string, object>> GetPerformanceOverviewAsync(Manager manager, string period);
    
    Task<(IEnumerable<Dictionary<string, object>> Clients, int TotalCount)> GetClientsCollectedAsync(Manager manager, string? search, int page, int pageSize);
} 