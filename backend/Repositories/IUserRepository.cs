using backend.Models;

namespace backend.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(long id);
    Task<User> AddAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByWorkIdAsync(string workId);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByWorkIdAsync(string workId);
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber);
    Task<bool> ExistsByNationalIdAsync(string nationalId);
    Task<int> CountByCreatedAtBetweenAsync(DateTime start, DateTime end);
    Task<int> CountByRoleAsync(Role role);
    Task<IEnumerable<User>> GetByRoleAsync(Role role);
}