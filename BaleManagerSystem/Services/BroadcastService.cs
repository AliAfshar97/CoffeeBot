using BaleManagerSystem.Models;
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

        public async Task<BroadcastResult> SendToUsers(
            List<long> chatIds,
            string message,
            string? fileId = null)
        {
            int success = 0;
            int failed = 0;

            var failedIds = new List<long>();

            var tasks = chatIds.Select(async chatId =>
            {
                try
                {
                    var response =
                        await _bot.SendMessage(chatId, message);

                    if (response != null && response.MessageId > 0)
                    {
                        Interlocked.Increment(ref success);
                    }
                    else
                    {
                        Interlocked.Increment(ref failed);

                        lock (failedIds)
                        {
                            failedIds.Add(chatId);
                        }
                    }
                }
                catch
                {
                    Interlocked.Increment(ref failed);

                    lock (failedIds)
                    {
                        failedIds.Add(chatId);
                    }
                }
            });

            await Task.WhenAll(tasks);

            return new BroadcastResult
            {
                SuccessCount = success,
                FailedCount = failed,
                FailedChatIds = failedIds
            };
        }
    }
}
