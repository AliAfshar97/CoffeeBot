using BaleManagerSystem.Models;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BaleManagerSystem.Services
{
    public class BaleUpdateHandler
    {
        private readonly IUserRepository _users;
        private readonly IOrderRepository _orders;
        private readonly IAccountRepository _accounts;
        private readonly ICoffeePriceRepository _prices;
        private readonly ReceiptFileService _receiptFiles;
        private readonly UserStateService _states;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BaleUpdateHandler> _logger;

        private static readonly Dictionary<string, string> DrinkLabels = new()
        {
            ["espresso"] = "Espresso",
            ["latte"] = "Latte",
            ["cappuccino"] = "Cappuccino",
            ["milk"] = "Milk"
        };

        private static readonly Dictionary<string, string> DrinkNamesPersian = new()
        {
            ["Espresso"] = "اسپرسو",
            ["Latte"] = "لاته",
            ["Cappuccino"] = "کاپوچینو",
            ["Milk"] = "شیر"
        };

        public BaleUpdateHandler(
            IUserRepository users,
            IOrderRepository orders,
            IAccountRepository accounts,
            ICoffeePriceRepository prices,
            ReceiptFileService receiptFiles,
            UserStateService states,
            IConfiguration configuration,
            ILogger<BaleUpdateHandler> logger)
        {
            _users = users;
            _orders = orders;
            _accounts = accounts;
            _prices = prices;
            _receiptFiles = receiptFiles;
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
                await HandleMessage(botClient, update.Message, ct);
            }

            if (update.CallbackQuery != null)
            {
                await HandleCallback(botClient, update.CallbackQuery);
            }
        }

        private async Task HandleMessage(
            ITelegramBotClient botClient,
            Message msg,
            CancellationToken ct)
        {
            var chatId = msg.Chat.Id;
            var text = (msg.Text ?? "").Trim();

            await _users.SaveUser(chatId, msg.Chat.Username ?? "");

            if (text == "/start")
            {
                _states.Remove(chatId);

                await botClient.SendMessage(
                    chatId,
                    "سلام! چه کاری انجام دهیم؟",
                    replyMarkup: BuildMainMenu());

                return;
            }

            if (_states.TryGet(chatId, out var state) &&
                state!.Step == ConversationStep.AwaitingReceiptPhoto)
            {
                if (msg.Photo != null && msg.Photo.Length > 0)
                {
                    await HandleReceiptPhotoAsync(botClient, msg, chatId, ct);
                    return;
                }

                await botClient.SendMessage(
                    chatId,
                    "لطفاً عکس رسید پرداخت خود را ارسال کنید.");

                return;
            }

            if (_states.TryGet(chatId, out state) &&
                state!.Step == ConversationStep.Name)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await botClient.SendMessage(
                        chatId,
                        "لطفاً نام خود را وارد کنید:");

                    return;
                }

                state.DisplayName = text;
                await _users.UpdateDisplayNameAsync(chatId, text);
                state.Step = ConversationStep.None;

                await botClient.SendMessage(
                    chatId,
                    $"{text} عزیز، ممنون! چند شات می‌خواهید؟",
                    replyMarkup: BuildShotMenu());

                return;
            }

            await botClient.SendMessage(
                chatId,
                "برای باز کردن منو /start را ارسال کنید.");
        }

        private async Task HandleReceiptPhotoAsync(
            ITelegramBotClient botClient,
            Message msg,
            long chatId,
            CancellationToken ct)
        {
            var user = await _users.GetUserByChatIdAsync(chatId);
            var displayName = user?.DisplayName ?? msg.Chat.Username ?? chatId.ToString();
            var photo = msg.Photo!.OrderByDescending(p => p.FileSize).First();

            var receipt = new PaymentReceipt
            {
                ChatId = chatId,
                DisplayName = displayName,
                TelegramFileId = photo.FileId,
                UserCaption = msg.Caption
            };

            var receiptId = await _accounts.CreateReceiptAsync(receipt);

            var localPath = await _receiptFiles.SaveTelegramPhotoAsync(
                botClient,
                photo,
                receiptId,
                ct);

            if (!string.IsNullOrEmpty(localPath))
            {
                await _accounts.UpdateReceiptFilePathAsync(receiptId, localPath);
            }

            _states.Remove(chatId);

            await botClient.SendMessage(
                chatId,
                "رسید پرداخت شما دریافت شد.\n" +
                "مدیر به زودی آن را بررسی و حساب شما را شارژ می‌کند.");

            await NotifyAdminReceiptAsync(botClient, receiptId, displayName, chatId, photo.FileId, msg.Caption);
        }

        private async Task HandleCallback(
            ITelegramBotClient botClient,
            CallbackQuery cb)
        {
            var chatId = cb.Message!.Chat.Id;
            var data = cb.Data ?? "";

            await botClient.AnswerCallbackQuery(cb.Id);

            if (data == "menu_order")
            {
                await botClient.SendMessage(
                    chatId,
                    "نوشیدنی مورد نظر را انتخاب کنید:",
                    replyMarkup: BuildDrinkMenu());

                return;
            }

            if (data == "menu_receipt")
            {
                var state = _states.GetOrCreate(chatId);
                state.Step = ConversationStep.AwaitingReceiptPhoto;
                state.DrinkType = string.Empty;
                state.ShotCount = 0;

                await botClient.SendMessage(
                    chatId,
                    "لطفاً عکس رسید پرداخت خود را ارسال کنید.\n" +
                    "در صورت نیاز می‌توانید توضیحات را در caption بنویسید.");

                return;
            }

            if (DrinkLabels.ContainsKey(data))
            {
                var state = _states.GetOrCreate(chatId);
                state.DrinkType = DrinkLabels[data];
                state.Step = ConversationStep.None;

                var user = await _users.GetUserByChatIdAsync(chatId);
                var drinkPersian = DrinkNamesPersian.GetValueOrDefault(state.DrinkType, state.DrinkType);

                if (user == null || string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    state.Step = ConversationStep.Name;

                    await botClient.SendMessage(
                        chatId,
                        "خوش آمدید! لطفاً نام خود را وارد کنید (برای سفارش‌های بعدی شما را می‌شناسیم):");
                }
                else
                {
                    state.DisplayName = user.DisplayName;

                    await botClient.SendMessage(
                        chatId,
                        $"{user.DisplayName} عزیز! برای {drinkPersian} چند شات می‌خواهید؟",
                        replyMarkup: BuildShotMenu());
                }

                return;
            }

            if (data is "shots_1" or "shots_2")
            {
                if (!_states.TryGet(chatId, out var state) ||
                    string.IsNullOrEmpty(state!.DrinkType))
                {
                    await botClient.SendMessage(
                        chatId,
                        "لطفاً /start را بزنید و «ثبت سفارش» را انتخاب کنید.",
                        replyMarkup: BuildMainMenu());

                    return;
                }

                state.ShotCount = data == "shots_1" ? (byte)1 : (byte)2;

                await botClient.SendMessage(
                    chatId,
                    "شکلات هم می‌خواهید؟",
                    replyMarkup: BuildChocolateMenu());

                return;
            }

            if (data is "choc_yes" or "choc_no")
            {
                if (!_states.TryGet(chatId, out var state) ||
                    string.IsNullOrEmpty(state!.DrinkType) ||
                    state.ShotCount == 0)
                {
                    await botClient.SendMessage(
                        chatId,
                        "لطفاً /start را بزنید و «ثبت سفارش» را انتخاب کنید.",
                        replyMarkup: BuildMainMenu());

                    return;
                }

                var withChocolate = data == "choc_yes";
                var displayName = state.DisplayName;

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    var user = await _users.GetUserByChatIdAsync(chatId);
                    displayName = user?.DisplayName ?? "نامشخص";
                }

                var price = await _prices.GetPriceAsync(
                    state.DrinkType,
                    state.ShotCount,
                    withChocolate) ?? 0;

                var drinkPersian = DrinkNamesPersian.GetValueOrDefault(state.DrinkType, state.DrinkType);

                var order = new CoffeeOrder
                {
                    ChatId = chatId,
                    DisplayName = displayName,
                    DrinkType = state.DrinkType,
                    ShotCount = state.ShotCount,
                    WithChocolate = withChocolate,
                    PriceInToman = price
                };

                var orderId = await _orders.SaveOrderAsync(order);

                await _accounts.AddDebitAsync(
                    chatId,
                    price,
                    $"سفارش: {drinkPersian} {state.ShotCount} شات",
                    orderId);

                var chocolateText = withChocolate ? "بله" : "خیر";

                await botClient.SendMessage(
                    chatId,
                    "سفارش شما ثبت شد!\n\n" +
                    $"نام: {displayName}\n" +
                    $"نوشیدنی: {drinkPersian}\n" +
                    $"شات: {state.ShotCount}\n" +
                    $"شکلات: {chocolateText}\n" +
                    $"مبلغ: {price:N0} تومان\n\n" +
                    "برای منوی اصلی /start را ارسال کنید.");

                await NotifyAdminAsync(botClient, order, chocolateText, drinkPersian);

                _states.Remove(chatId);
            }
        }

        private async Task NotifyAdminReceiptAsync(
            ITelegramBotClient botClient,
            int receiptId,
            string displayName,
            long chatId,
            string fileId,
            string? caption)
        {
            var adminChatIdSetting = _configuration["BaleSettings:AdminBotId"];

            if (string.IsNullOrEmpty(adminChatIdSetting))
                return;

            try
            {
                var adminChatId = long.Parse(adminChatIdSetting);

                await botClient.SendMessage(
                    adminChatId,
                    "رسید پرداخت جدید\n\n" +
                    $"شماره رسید: {receiptId}\n" +
                    $"نام: {displayName}\n" +
                    $"شناسه چت: {chatId}\n" +
                    $"توضیحات: {caption ?? "-"}");

                await botClient.SendPhoto(
                    adminChatId,
                    InputFile.FromFileId(fileId),
                    caption: $"رسید #{receiptId} از {displayName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify admin about payment receipt");
            }
        }

        private async Task NotifyAdminAsync(
            ITelegramBotClient botClient,
            CoffeeOrder order,
            string chocolateText,
            string drinkPersian)
        {
            var adminChatIdSetting = _configuration["BaleSettings:AdminBotId"];

            if (string.IsNullOrEmpty(adminChatIdSetting))
                return;

            try
            {
                var adminChatId = long.Parse(adminChatIdSetting);

                await botClient.SendMessage(
                    adminChatId,
                    "سفارش قهوه جدید\n\n" +
                    $"نام: {order.DisplayName}\n" +
                    $"شناسه چت: {order.ChatId}\n" +
                    $"نوشیدنی: {drinkPersian}\n" +
                    $"شات: {order.ShotCount}\n" +
                    $"شکلات: {chocolateText}\n" +
                    $"مبلغ: {order.PriceInToman:N0} تومان");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify admin about new order");
            }
        }

        private static InlineKeyboardMarkup BuildMainMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("ثبت سفارش", "menu_order")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("ارسال رسید پرداخت", "menu_receipt")
                }
            });
        }

        private static InlineKeyboardMarkup BuildDrinkMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("اسپرسو", "espresso"),
                    InlineKeyboardButton.WithCallbackData("لاته", "latte")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("کاپوچینو", "cappuccino"),
                    InlineKeyboardButton.WithCallbackData("شیر", "milk")
                }
            });
        }

        private static InlineKeyboardMarkup BuildShotMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("۱ شات", "shots_1"),
                    InlineKeyboardButton.WithCallbackData("۲ شات", "shots_2")
                }
            });
        }

        private static InlineKeyboardMarkup BuildChocolateMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("با شکلات", "choc_yes"),
                    InlineKeyboardButton.WithCallbackData("بدون شکلات", "choc_no")
                }
            });
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
