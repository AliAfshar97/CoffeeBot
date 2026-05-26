using Telegram.Bot;

namespace BaleManagerSystem.Services
{
    public class BroadcastService
    {
        private readonly IUserRepository _users;
        private readonly ITelegramBotClient _bot;

        public BroadcastService(
            IUserRepository users,
            ITelegramBotClient bot)
        {
            _users = users;
            _bot = bot;
        }

        public async Task SendToAll(string message)
        {
            var users = await _users.GetAllChatIds();

            var tasks = users.Select(async x =>
            {
                try
                {
                    await _bot.SendMessage(x, message);
                }
                catch
                {
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
