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
                    "Welcome! What would you like to do?",
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
                    "Please send a photo of your payment receipt.");

                return;
            }

            if (_states.TryGet(chatId, out state) &&
                state!.Step == ConversationStep.Name)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    await botClient.SendMessage(
                        chatId,
                        "Please enter your name:");

                    return;
                }

                state.DisplayName = text;
                await _users.UpdateDisplayNameAsync(chatId, text);
                state.Step = ConversationStep.None;

                await botClient.SendMessage(
                    chatId,
                    $"Thanks {text}! How many shots?",
                    replyMarkup: BuildShotMenu());

                return;
            }

            await botClient.SendMessage(
                chatId,
                "Send /start to open the menu.");
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
                "Your payment receipt was received.\n" +
                "The admin will review it and charge your account soon.");

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
                    "Choose your drink:",
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
                    "Please send a photo of your payment receipt.\n" +
                    "You can add a caption with extra details if needed.");

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
                        "Welcome! Please enter your name (we will remember you for next orders):");
                }
                else
                {
                    state.DisplayName = user.DisplayName;

                    await botClient.SendMessage(
                        chatId,
                        $"Hi {user.DisplayName}! How many shots for your {state.DrinkType}?",
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
                        "Please send /start and choose Place Order.",
                        replyMarkup: BuildMainMenu());

                    return;
                }

                state.ShotCount = data == "shots_1" ? (byte)1 : (byte)2;

                await botClient.SendMessage(
                    chatId,
                    "Would you like chocolate?",
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
                        "Please send /start and choose Place Order.",
                        replyMarkup: BuildMainMenu());

                    return;
                }

                var withChocolate = data == "choc_yes";
                var displayName = state.DisplayName;

                if (string.IsNullOrWhiteSpace(displayName))
                {
                    var user = await _users.GetUserByChatIdAsync(chatId);
                    displayName = user?.DisplayName ?? "Unknown";
                }

                var price = await _prices.GetPriceAsync(
                    state.DrinkType,
                    state.ShotCount,
                    withChocolate) ?? 0;

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
                    $"Order: {state.DrinkType} {state.ShotCount} shot(s)",
                    orderId);

                var chocolateText = withChocolate ? "Yes" : "No";

                await botClient.SendMessage(
                    chatId,
                    "Your order has been saved!\n\n" +
                    $"Name: {displayName}\n" +
                    $"Drink: {state.DrinkType}\n" +
                    $"Shots: {state.ShotCount}\n" +
                    $"Chocolate: {chocolateText}\n" +
                    $"Price: {price:N0} Toman\n\n" +
                    "Send /start to open the menu again.");

                await NotifyAdminAsync(botClient, order, chocolateText);

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
                    "New payment receipt\n\n" +
                    $"Receipt #: {receiptId}\n" +
                    $"Name: {displayName}\n" +
                    $"Chat ID: {chatId}\n" +
                    $"Caption: {caption ?? "-"}");

                await botClient.SendPhoto(
                    adminChatId,
                    InputFile.FromFileId(fileId),
                    caption: $"Receipt #{receiptId} from {displayName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify admin about payment receipt");
            }
        }

        private async Task NotifyAdminAsync(
            ITelegramBotClient botClient,
            CoffeeOrder order,
            string chocolateText)
        {
            var adminChatIdSetting = _configuration["BaleSettings:AdminBotId"];

            if (string.IsNullOrEmpty(adminChatIdSetting))
                return;

            try
            {
                var adminChatId = long.Parse(adminChatIdSetting);

                await botClient.SendMessage(
                    adminChatId,
                    "New coffee order\n\n" +
                    $"Name: {order.DisplayName}\n" +
                    $"Chat ID: {order.ChatId}\n" +
                    $"Drink: {order.DrinkType}\n" +
                    $"Shots: {order.ShotCount}\n" +
                    $"Chocolate: {chocolateText}\n" +
                    $"Price: {order.PriceInToman:N0} Toman");
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
                    InlineKeyboardButton.WithCallbackData("Place Order", "menu_order")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Send Payment Receipt", "menu_receipt")
                }
            });
        }

        private static InlineKeyboardMarkup BuildDrinkMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Espresso", "espresso"),
                    InlineKeyboardButton.WithCallbackData("Latte", "latte")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Cappuccino", "cappuccino"),
                    InlineKeyboardButton.WithCallbackData("Milk", "milk")
                }
            });
        }

        private static InlineKeyboardMarkup BuildShotMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("1 Shot", "shots_1"),
                    InlineKeyboardButton.WithCallbackData("2 Shots", "shots_2")
                }
            });
        }

        private static InlineKeyboardMarkup BuildChocolateMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("With Chocolate", "choc_yes"),
                    InlineKeyboardButton.WithCallbackData("No Chocolate", "choc_no")
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
