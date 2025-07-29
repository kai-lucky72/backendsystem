using backend.Models;

namespace backend.Repositories;

public interface IGroupRepository
{
    Task<IEnumerable<Group>> GetAllAsync();
    Task<Group?> GetByIdAsync(long id);
    Task<Group> AddAsync(Group group);
    Task<Group> UpdateAsync(Group group);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<IEnumerable<Group>> GetByManagerAsync(Manager manager);
    Task<bool> ExistsByManagerAndNameAsync(Manager manager, string name);
    Task<Group?> GetByIdWithAgentsAsync(long id);
}