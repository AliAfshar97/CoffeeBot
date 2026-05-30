using BaleManagerSystem.Models.ViewModels;

namespace BaleManagerSystem.Services
{
    public interface IUserRepository
    {
        Task SaveUser(long chatId, string username);

        Task<List<UserChatIdViewModel>> GetAllChatIds();
    }
}
