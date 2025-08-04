using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace backend.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(ApplicationDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == registerDto.Email || u.WorkId == registerDto.WorkId);
        
        if (existingUser != null)
        {
            return new AuthResponseDto 
            { 
                Success = false, 
                Message = "User with this email or work ID already exists" 
            };
        }

        var user = new User
        {
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            WorkId = registerDto.WorkId,
            Role = registerDto.Role,
            CreatedAt = DateTime.UtcNow,
            PasswordHash = HashPassword(registerDto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return new AuthResponseDto 
        { 
            Success = true, 
            Token = token, 
            User = new UserDto 
            { 
                Id = user.Id, 
                Email = user.Email, 
                FirstName = user.FirstName, 
                LastName = user.LastName,
                WorkId = user.WorkId,
                Role = user.Role
            } 
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.WorkId == loginDto.WorkId);
        
        if (user == null)
        {
            return new AuthResponseDto { Success = false, Message = "Invalid credentials" };
        }

        if (!VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            return new AuthResponseDto { Success = false, Message = "Invalid credentials" };
        }

        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return new AuthResponseDto 
        { 
            Success = true, 
            Token = token, 
            User = new UserDto 
            { 
                Id = user.Id, 
                Email = user.Email, 
                FirstName = user.FirstName, 
                LastName = user.LastName,
                WorkId = user.WorkId,
                Role = user.Role
            } 
        };
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(long.Parse(userId));
        if (user == null) return false;

        if (!VerifyPassword(currentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return false;

        // Here you would typically send a reset token via email
        // For now, we'll just return true
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return false;

        // Here you would validate the token
        // For now, we'll just update the password
        user.PasswordHash = HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        var hashedPassword = HashPassword(password);
        return hashedPassword == hash;
    }
}