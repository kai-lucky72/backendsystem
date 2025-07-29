using backend.Models;

namespace backend.Repositories;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetAllAsync();
    Task<Notification?> GetByIdAsync(long id);
    Task<Notification> AddAsync(Notification notification);
    Task<Notification> UpdateAsync(Notification notification);
    Task DeleteAsync(long id);
    
    // Custom methods from Java repository
    Task<IEnumerable<Notification>> GetByRecipientOrderBySentAtDescAsync(User recipient);
    Task<IEnumerable<Notification>> GetBySenderOrderBySentAtDescAsync(User sender);
    Task<IEnumerable<Notification>> GetByRecipientIsNullOrderBySentAtDescAsync();
}