using BaleManagerSystem.Models;

namespace BaleManagerSystem.Services
{
    public interface IMenuRepository
    {
        // All items, including inactive ones (admin view).
        Task<List<MenuItem>> GetAllAsync();

        // Active items ordered for display (bot menu).
        Task<List<MenuItem>> GetActiveOrderedAsync();

        // Active items visible to a user, based on their subscription status.
        Task<List<MenuItem>> GetActiveForSubscriberAsync(bool isSubscriber);

        Task<MenuItem?> GetByIdAsync(int id);

        Task<MenuItem?> GetByKeyAsync(string itemKey);

        Task<bool> KeyExistsAsync(string itemKey, int? excludeId = null);

        Task<int> CreateAsync(MenuItem item);

        Task UpdateAsync(MenuItem item);

        // Soft delete: keeps the row so past orders still resolve their name.
        Task SetActiveAsync(int id, bool isActive);
    }
}
