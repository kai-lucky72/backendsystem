using backend.Models;

namespace backend.Repositories;

public interface IManagerRepository
{
    Task<IEnumerable<Manager>> GetAllAsync();
    Task<Manager?> GetByIdAsync(long id);
    Task<Manager> AddAsync(Manager manager);
    Task<Manager> UpdateAsync(Manager manager);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<IEnumerable<Manager>> GetByCreatedByAsync(User admin);
}