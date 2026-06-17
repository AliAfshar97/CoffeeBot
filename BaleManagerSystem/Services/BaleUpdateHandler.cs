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
            ["milk"] = "Milk",
            ["chocolate"] = "Chocolate"
        };

        private static readonly Dictionary<string, string> DrinkNamesPersian = new()
        {
            ["Espresso"] = "اسپرسو",
            ["Latte"] = "لاته",
            ["Cappuccino"] = "کاپوچینو",
            ["Milk"] = "شیر",
            ["Chocolate"] = "شکلات"
        };

        private const string ChocolateDrinkType = "Chocolate";
        private const string MilkDrinkType = "Milk";

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
            try
            {
                if (update.Message != null)
                {
                    await HandleMessage(botClient, update.Message, ct);
                }

                if (update.CallbackQuery != null)
                {
                    await HandleCallback(botClient, update.CallbackQuery, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle Bale update {UpdateId}", update.Id);

                if (update.Message?.Chat.Id is long chatId)
                {
                    try
                    {
                        await botClient.SendMessage(
                            chatId,
                            "خطایی رخ داد. لطفاً دوباره /start را ارسال کنید.",
                            cancellationToken: ct);
                    }
                    catch (Exception sendEx)
                    {
                        _logger.LogError(sendEx, "Failed to send error message to chat {ChatId}", chatId);
                    }
                }
            }
        }

        private static bool IsStartCommand(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var command = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];

            return command.Equals("/start", StringComparison.OrdinalIgnoreCase) ||
                   command.StartsWith("/start@", StringComparison.OrdinalIgnoreCase);
        }

        private async Task HandleMessage(
            ITelegramBotClient botClient,
            Message msg,
            CancellationToken ct)
        {
            var chatId = msg.Chat.Id;
            var text = (msg.Text ?? "").Trim();

            if (IsStartCommand(text))
            {
                _states.Remove(chatId);

                await TrySaveUserAsync(chatId, msg.Chat.Username ?? "", ct);

                await botClient.SendMessage(
                    chatId,
                    "سلام! چه کاری انجام دهیم؟",
                    replyMarkup: BuildMainMenu(),
                    cancellationToken: ct);

                return;
            }

            await TrySaveUserAsync(chatId, msg.Chat.Username ?? "", ct);

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

                if (IsChocolateDrink(state.DrinkType))
                {
                    state.ShotCount = 1;
                    await CompleteOrderAsync(botClient, chatId, state, withChocolate: false, ct);
                    return;
                }

                await botClient.SendMessage(
                    chatId,
                    BuildQuantityPrompt(text, state.DrinkType, isNameJustEntered: true),
                    replyMarkup: BuildQuantityMenu(state.DrinkType),
                    cancellationToken: ct);

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

        private async Task TrySaveUserAsync(long chatId, string username, CancellationToken ct)
        {
            try
            {
                await _users.SaveUser(chatId, username);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not save user {ChatId} to database", chatId);
            }
        }

        private async Task HandleCallback(
            ITelegramBotClient botClient,
            CallbackQuery cb,
            CancellationToken ct)
        {
            var chatId = cb.Message!.Chat.Id;
            var data = cb.Data ?? "";

            await botClient.AnswerCallbackQuery(cb.Id, cancellationToken: ct);

            if (data == "menu_order")
            {
                await botClient.SendMessage(
                    chatId,
                    "خیلی ام عالی دوست من! خوشحالیم اینجایی چی میل داری؟",
                    replyMarkup: BuildDrinkMenu(),
                    cancellationToken: ct);

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
                    "در صورت نیاز می‌توانید توضیحات را در caption بنویسید.",
                    cancellationToken: ct);

                return;
            }

            if (DrinkLabels.ContainsKey(data))
            {
                var state = _states.GetOrCreate(chatId);
                state.DrinkType = DrinkLabels[data];
                state.Step = ConversationStep.None;

                var user = await _users.GetUserByChatIdAsync(chatId);

                if (user == null || string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    state.Step = ConversationStep.Name;

                    await botClient.SendMessage(
                        chatId,
                        "خوش آمدید! لطفاً نام خود را وارد کنید (برای سفارش‌های بعدی شما را می‌شناسیم):",
                        cancellationToken: ct);
                }
                else
                {
                    state.DisplayName = user.DisplayName;

                    if (IsChocolateDrink(state.DrinkType))
                    {
                        state.ShotCount = 1;
                        await CompleteOrderAsync(botClient, chatId, state, withChocolate: false, ct);
                        return;
                    }

                    await botClient.SendMessage(
                        chatId,
                        BuildQuantityPrompt(user.DisplayName, state.DrinkType, isNameJustEntered: false),
                        replyMarkup: BuildQuantityMenu(state.DrinkType),
                        cancellationToken: ct);
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
                        replyMarkup: BuildMainMenu(),
                        cancellationToken: ct);

                    return;
                }

                state.ShotCount = data == "shots_1" ? (byte)1 : (byte)2;

                if (IsChocolateDrink(state.DrinkType))
                {
                    await CompleteOrderAsync(botClient, chatId, state, withChocolate: false, ct);
                    return;
                }

                await botClient.SendMessage(
                    chatId,
                    "شکلات هم می‌خواهید؟",
                    replyMarkup: BuildChocolateMenu(),
                    cancellationToken: ct);

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
                        replyMarkup: BuildMainMenu(),
                        cancellationToken: ct);

                    return;
                }

                var withChocolate = data == "choc_yes";
                await CompleteOrderAsync(botClient, chatId, state, withChocolate, ct);
            }
        }

        private async Task CompleteOrderAsync(
            ITelegramBotClient botClient,
            long chatId,
            UserState state,
            bool withChocolate,
            CancellationToken ct)
        {
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
                BuildDebitDescription(drinkPersian, state.DrinkType, state.ShotCount, withChocolate),
                orderId);

            var confirmation = BuildOrderConfirmation(
                displayName,
                drinkPersian,
                state.DrinkType,
                state.ShotCount,
                withChocolate,
                price);

            await botClient.SendMessage(
                chatId,
                confirmation,
                cancellationToken: ct);

            if (!IsChocolateDrink(state.DrinkType))
            {
                var chocolateText = withChocolate ? "بله" : "خیر";
                await NotifyAdminAsync(botClient, order, chocolateText, drinkPersian);
            }
            else
            {
                await NotifyAdminAsync(botClient, order, "-", drinkPersian);
            }

            _states.Remove(chatId);
        }

        private static bool IsChocolateDrink(string drinkType) =>
            drinkType == ChocolateDrinkType;

        private static bool UsesCups(string drinkType) =>
            drinkType == MilkDrinkType;

        private static string BuildQuantityPrompt(
            string displayName,
            string drinkType,
            bool isNameJustEntered)
        {
            var drinkPersian = DrinkNamesPersian.GetValueOrDefault(drinkType, drinkType);

            if (UsesCups(drinkType))
            {
                return isNameJustEntered
                    ? $"{displayName} عزیز، ممنون! چند لیوان می‌خواهید؟"
                    : $"{displayName} عزیز! برای {drinkPersian} چند لیوان می‌خواهید؟";
            }

            return isNameJustEntered
                ? $"{displayName} عزیز، ممنون! برای {drinkPersian} چند شات می‌خواهید؟"
                : $"{displayName} عزیز! برای {drinkPersian} چند شات می‌خواهید؟";
        }

        private static string BuildDebitDescription(
            string drinkPersian,
            string drinkType,
            byte shotCount,
            bool withChocolate)
        {
            if (IsChocolateDrink(drinkType))
                return $"سفارش: {drinkPersian}";

            var unit = UsesCups(drinkType) ? "لیوان" : "شات";
            var description = $"سفارش: {drinkPersian} {shotCount} {unit}";

            if (!IsChocolateDrink(drinkType))
                description += withChocolate ? " با شکلات" : "";

            return description;
        }

        private static string BuildOrderConfirmation(
            string displayName,
            string drinkPersian,
            string drinkType,
            byte shotCount,
            bool withChocolate,
            int price)
        {
            var lines = new List<string>
            {
                "سفارش شما ثبت شد!",
                "",
                $"نام: {displayName}",
                $"نوشیدنی: {drinkPersian}"
            };

            if (!IsChocolateDrink(drinkType))
            {
                var unit = UsesCups(drinkType) ? "لیوان" : "شات";
                lines.Add($"{unit}: {shotCount}");
                lines.Add($"شکلات: {(withChocolate ? "بله" : "خیر")}");
            }

            lines.Add($"مبلغ: {price:N0} تومان");
            lines.Add("");
            lines.Add("برای منوی اصلی /start را ارسال کنید.");

            return string.Join("\n", lines);
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
                    BuildAdminQuantityLine(order) +
                    BuildAdminChocolateLine(order, chocolateText) +
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

        private static string BuildAdminQuantityLine(CoffeeOrder order)
        {
            if (IsChocolateDrink(order.DrinkType))
                return string.Empty;

            var unit = UsesCups(order.DrinkType) ? "لیوان" : "شات";
            return $"{unit}: {order.ShotCount}\n";
        }

        private static string BuildAdminChocolateLine(CoffeeOrder order, string chocolateText)
        {
            if (IsChocolateDrink(order.DrinkType))
                return string.Empty;

            return $"شکلات: {chocolateText}\n";
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
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("شکلات", "chocolate")
                }
            });
        }

        private static InlineKeyboardMarkup BuildQuantityMenu(string drinkType)
        {
            if (UsesCups(drinkType))
            {
                return new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("۱ لیوان", "shots_1"),
                        InlineKeyboardButton.WithCallbackData("۲ لیوان", "shots_2")
                    }
                });
            }

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
