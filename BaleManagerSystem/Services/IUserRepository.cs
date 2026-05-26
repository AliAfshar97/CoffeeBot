namespace BaleManagerSystem.Services
{
    public interface IUserRepository
    {
        Task SaveUser(long chatId, string username);

        Task<List<long>> GetAllChatIds();
    }
}
