using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Services;

public interface IUserService
{
    Task<User> CreateUserAsync(string firstName, string lastName, string phoneNumber, string nationalId,
                              string email, string workId, string? password, User.Role role, User createdBy);
    
    Task<User> GetUserByIdAsync(long id);
    
    Task<User> GetUserByEmailAsync(string email);
    
    Task<User> GetUserByWorkIdAsync(string workId);
    
    Task<IEnumerable<UserDTO>> GetAllUsersAsync();
    
    Task<User> UpdateUserStatusAsync(long id, bool active);
    
    Task<bool> ResetPasswordAsync(long id, string newPassword);

    Task<int> CountUsersByRoleAsync(User.Role role);
    
    /// <summary>
    /// Checks if an email address is already taken by another user
    /// </summary>
    /// <param name="email">The email address to check</param>
    /// <returns>true if the email is already in use, false otherwise</returns>
    Task<bool> IsEmailTakenAsync(string email);
    
    /// <summary>
    /// Checks if a work ID is already taken by another user
    /// </summary>
    /// <param name="workId">The work ID to check</param>
    /// <returns>true if the work ID is already in use, false otherwise</returns>
    Task<bool> IsWorkIdTakenAsync(string workId);

    /// <summary>
    /// Checks if a phone number is already taken by another user
    /// </summary>
    /// <param name="phoneNumber">The phone number to check</param>
    /// <returns>true if the phone number is already in use, false otherwise</returns>
    Task<bool> IsPhoneNumberTakenAsync(string phoneNumber);
}