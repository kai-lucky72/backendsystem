using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        try
        {
            var users = await _context.Users.ToListAsync();
            
            // Manually load related entities to avoid data type issues
            foreach (var user in users)
            {
                try
                {
                    await _context.Entry(user).Reference(u => u.Agent).LoadAsync();
                }
                catch
                {
                    // Ignore agent loading errors
                }
                
                try
                {
                    await _context.Entry(user).Reference(u => u.Manager).LoadAsync();
                }
                catch
                {
                    // Ignore manager loading errors
                }
            }
            
            return users;
        }
        catch (Exception ex)
        {
            // Fallback to simple query without related entities
            return await _context.Users.ToListAsync();
        }
    }

    public async Task<User?> GetByIdAsync(long id)
    {
        return await _context.Users
            .Include(u => u.Agent)
            .Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User> AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task DeleteAsync(long id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Agent)
            .Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByWorkIdAsync(string workId)
    {
        try
        {
            // First try a simple query without joins
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.WorkId == workId);
            
            if (user != null)
            {
                // Manually load related entities to avoid data type issues
                try
                {
                    await _context.Entry(user).Reference(u => u.Agent).LoadAsync();
                }
                catch
                {
                    // Ignore agent loading errors
                }
                
                try
                {
                    await _context.Entry(user).Reference(u => u.Manager).LoadAsync();
                }
                catch
                {
                    // Ignore manager loading errors
                }
            }
            
            return user;
        }
        catch (Exception ex)
        {
            // Final fallback - use raw SQL
            try
            {
                var user = await _context.Users
                    .FromSqlRaw("SELECT * FROM users WHERE work_id = {0}", workId)
                    .FirstOrDefaultAsync();
                
                return user;
            }
            catch
            {
                return null;
            }
        }
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByWorkIdAsync(string workId)
    {
        return await _context.Users
            .AnyAsync(u => u.WorkId == workId);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Users
            .AnyAsync(u => u.PhoneNumber == phoneNumber);
    }

    public async Task<bool> ExistsByNationalIdAsync(string nationalId)
    {
        return await _context.Users
            .AnyAsync(u => u.NationalId == nationalId);
    }

    public async Task<int> CountByCreatedAtBetweenAsync(DateTime start, DateTime end)
    {
        return await _context.Users
            .CountAsync(u => u.CreatedAt >= start && u.CreatedAt <= end);
    }

    public async Task<int> CountByRoleAsync(Role role)
    {
        return await _context.Users
            .CountAsync(u => u.Role == role);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(Role role)
    {
        return await _context.Users
            .Include(u => u.Agent)
            .Include(u => u.Manager)
            .Where(u => u.Role == role)
            .ToListAsync();
    }
}