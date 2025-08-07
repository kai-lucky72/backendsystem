using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Notification>> GetAllAsync()
    {
        return await _context.Notifications
            .Include(n => n.Sender)
            .Include(n => n.Recipient)
            .ToListAsync();
    }

    public async Task<Notification?> GetByIdAsync(long id)
    {
        return await _context.Notifications
            .Include(n => n.Sender)
            .Include(n => n.Recipient)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<Notification> AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<Notification> UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task DeleteAsync(long id)
    {
        var notification = await GetByIdAsync(id);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Notification>> GetByRecipientOrderBySentAtDescAsync(User recipient)
    {
        return await _context.Notifications
            .Include(n => n.Sender)
            .Include(n => n.Recipient)
            .Where(n => n.RecipientId == recipient.Id)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetBySenderOrderBySentAtDescAsync(User sender)
    {
        return await _context.Notifications
            .Include(n => n.Sender)
            .Include(n => n.Recipient)
            .Where(n => n.SenderId == sender.Id)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetByRecipientIsNullOrderBySentAtDescAsync()
    {
        return await _context.Notifications
            .Include(n => n.Sender)
            .Include(n => n.Recipient)
            .Where(n => n.RecipientId == null)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();
    }

    // Add the missing CountAllAsync method
    public async Task<int> CountAllAsync()
    {
        return await _context.Notifications.CountAsync();
    }
}