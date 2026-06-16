using BaleManagerSystem.Models;
using BaleManagerSystem.Models.ViewModels;

namespace BaleManagerSystem.Services
{
    public interface IUserRepository
    {
        Task SaveUser(long chatId, string username);

        Task<int> GetUserCountAsync();

        Task<ChatUser?> GetUserByChatIdAsync(long chatId);

        Task UpdateDisplayNameAsync(long chatId, string displayName);

        Task<List<UserChatIdViewModel>> GetAllChatIds();
    }
}
