using BaleManagerSystem.Models;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BaleManagerSystem.Services
{
    public class BroadcastService
    {
        private readonly IUserRepository _users;
        private readonly ITelegramBotClient _bot;
        private readonly BaleMessageService _baleMessageService;

        public BroadcastService(
            IUserRepository users,
            ITelegramBotClient bot,
            BaleMessageService baleMessageService)
        {
            _users = users;
            _bot = bot;
            _baleMessageService = baleMessageService;
        }

        public async Task<BroadcastResult> SendToUsers(
            List<long> chatIds,
            string message,
            string? fileId = null)
        {
            int success = 0;
            int failed = 0;

            var failedIds = new List<long>();

            foreach (var chatId in chatIds)
            {
                try
                {
                    Telegram.Bot.Types.Message response;

                    if (fileId != null)
                    {
                        var responseSend = await _baleMessageService.SendPhotoAsync(
                            chatId: chatId,
                            text: message,
                            fileId : fileId);

                        if (responseSend)
                        {
                            success++;
                        }
                        else
                        {
                            failed++;
                            failedIds.Add(chatId);
                        }
                    }
                    else
                    {
                        response =
                            await _bot.SendMessage(
                                chatId,
                                message);

                        if (response != null &&
                        response.MessageId > 0)
                        {
                            success++;
                        }
                        else
                        {
                            failed++;
                            failedIds.Add(chatId);
                        }
                    }
                }
                catch
                {
                    failed++;
                    failedIds.Add(chatId);
                }
            }

            return new BroadcastResult
            {
                SuccessCount = success,
                FailedCount = failed,
                FailedChatIds = failedIds
            };
        }
    }
}
