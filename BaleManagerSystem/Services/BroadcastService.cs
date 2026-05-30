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

        public async Task SendToUsers(
            List<long> chatIds,
            string message)
        {
            var tasks = chatIds.Select(async chatId =>
            {
                try
                {
                    await _bot.SendMessage(
                        chatId,
                        message);
                }
                catch
                {
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
