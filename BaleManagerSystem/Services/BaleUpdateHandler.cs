using BaleManagerSystem.Models;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BaleManagerSystem.Services
{
    public class BaleUpdateHandler
    {
        private readonly IUserRepository _users;
        private readonly IConsultationRepository _consultations;
        private readonly UserStateService _states;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BaleUpdateHandler> _logger;

        public BaleUpdateHandler(
            IUserRepository users,
            IConsultationRepository consultations,
            UserStateService states,
            IConfiguration configuration,
            ILogger<BaleUpdateHandler> logger)
        {
            _users = users;
            _consultations = consultations;
            _states = states;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken ct)
        {
            if (update.Message != null)
            {
                await HandleMessage(botClient, update.Message);
            }

            if (update.CallbackQuery != null)
            {
                await HandleCallback(botClient, update.CallbackQuery);
            }
        }

        private async Task  HandleMessage(
            ITelegramBotClient botClient,
            Message msg)
        {
            var chatId = msg.Chat.Id;
            var text = msg.Text ?? "";

            await _users.SaveUser(chatId, msg.Chat.Username ?? "");

            // START COMMAND
            if (text == "/start")
            {
                var menu = new InlineKeyboardMarkup(new[]
                {
                     new[]
                     {
                         InlineKeyboardButton.WithCallbackData(
                             "راهکار های ERP سازمانی",
                             "erp")
                     },

                     new[]
                     {
                         InlineKeyboardButton.WithCallbackData(
                             "مهاجرت به نسخه تحت وب",
                             "migration")
                     },

                     new[]
                     {
                         InlineKeyboardButton.WithCallbackData(
                             "راهکارهای دورکاری TSPLUS",
                             "TSPLUS")
                     },

                     new[]
                     {
                         InlineKeyboardButton.WithCallbackData(
                             "سایر موارد",
                             "other")
                     }
                });

                await botClient.SendMessage(
                    chatId,
                    "موضوع مورد نظر را انتخاب کنید :",
                    replyMarkup: menu);

                return;
            }

            // HANDLE USER STATE
            if (_states.TryGet(chatId, out var state))
            {
                switch (state!.Step)
                {
                    case ConversationStep.ShortBrief:

                        state.ShortBrief = text;

                        InlineKeyboardMarkup menu = new InlineKeyboardMarkup();
                        menu = new InlineKeyboardMarkup(new[]
                        {
                              new[]
                              {
                                  InlineKeyboardButton.WithCallbackData(
                                      "تماس با کارشناسان حساب رایان",
                                      "contact")
                              },

                              new[]
                              {
                                  InlineKeyboardButton.WithCallbackData(
                                      "ثبت درخواست مشاوره",
                                      "register")
                              }
                        });

                        await botClient.SendMessage(
                            chatId,
                            "یکی از گزینه‌ها را انتخاب کنید :",
                            replyMarkup: menu);

                        return;
                    case ConversationStep.Name:

                        state.FullName = text;

                        state.Step = ConversationStep.Phone;

                        await botClient.SendMessage(
                            chatId,
                            "لطفا شماره تماس خود را وارد کنید :");

                        return;

                    case ConversationStep.Phone:

                        if (!Regex.IsMatch(text, @"^09\d{9}$"))
                        {
                            await botClient.SendMessage(
                                chatId,
                                "شماره تلفن را با اعداد انگلیسی و به صورت صحیح وارد کنید.");

                            return;
                        }

                        state.Phone = text;

                        state.Step = ConversationStep.Company;

                        await botClient.SendMessage(
                            chatId,
                            "نام شرکت خود را وارد کنید :");

                        return;

                    case ConversationStep.Company:

                        state.Company = text;

                        await _consultations.SaveConsultation(
                            new Consultation
                            {
                                ChatId = chatId,
                                FullName = state.FullName,
                                PhoneNumber = state.Phone,
                                Company = state.Company,
                                ShortBrief = state.ShortBrief,  
                                Category = state.Category
                            });

                        long adminChatId = long.Parse(
                            _configuration["BaleSettings:AdminBotId"]!);

                        await botClient.SendMessage(
                            adminChatId,
                            $"📥 پیام جدید \n\n" +
                            $"نام : {state.FullName}\n" +
                            $"شماره تلفن : {state.Phone}\n" +
                            $"نام شرکت: {state.Company}\n" +
                            $"دسته بندی: {state.Category}");

                        await botClient.SendMessage(
                            chatId,
                            "درخواست شما ثبت شد ✅");

                        _states.Remove(chatId);

                        return;
                }
            }
        }

        private async Task HandleCallback(
            ITelegramBotClient botClient,
            CallbackQuery cb)
        {
            var chatId = cb.Message!.Chat.Id;

            var data = cb.Data;

            // CATEGORY SELECTION
            if (data is "erp" or "migration" or "TSPLUS" or "other")
            {
                var state = _states.GetOrCreate(chatId);

                state.Category = data!;

                InlineKeyboardMarkup menu = new InlineKeyboardMarkup();

                if (data == "other")
                {
                    state.Step = ConversationStep.ShortBrief;

                    await botClient.SendMessage(
                            chatId,
                            "لطفا موضوع مورد نظر خود را به صورت کوتاه ارسال کنید تا درخواست شما دقیق تر ثبت شود.");
                }
                else
                {
                    menu = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "تماس با کارشناسان حساب رایان",
                                "contact")
                        },

                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "ثبت درخواست مشاوره",
                                "register")
                        }
                    });

                    await botClient.SendMessage(
                        chatId,
                        "یکی از گزینه‌ها را انتخاب کنید :",
                        replyMarkup: menu);
                }
                  
                await botClient.AnswerCallbackQuery(cb.Id);

                return;
            }

            // REGISTER CONSULTATION
            if (data == "register")
            {
                var state = _states.GetOrCreate(chatId);

                state.Step = ConversationStep.Name;

                await botClient.SendMessage(
                    chatId,
                    "نام و نام خانوادگی خود را وارد کنید :");

                await botClient.AnswerCallbackQuery(cb.Id);

                return;
            }

            // CONTACT
            if (data == "contact")
            {
                await botClient.SendMessage(
                    chatId,
                    "☎ 02187760\n📱 09101087760\n@hesabrayandm");

                await botClient.AnswerCallbackQuery(cb.Id);

                return;
            }
        }

        public Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken ct)
        {
            _logger.LogError(exception, "Bale Bot Error");

            return Task.CompletedTask;
        }
    }
}
