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
        private readonly IMenuRepository _menu;
        private readonly ReceiptFileService _receiptFiles;
        private readonly UserStateService _states;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BaleUpdateHandler> _logger;

        public BaleUpdateHandler(
            IUserRepository users,
            IOrderRepository orders,
            IAccountRepository accounts,
            ICoffeePriceRepository prices,
            IMenuRepository menu,
            ReceiptFileService receiptFiles,
            UserStateService states,
            IConfiguration configuration,
            ILogger<BaleUpdateHandler> logger)
        {
            _users = users;
            _orders = orders;
            _accounts = accounts;
            _prices = prices;
            _menu = menu;
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
                var receiptFileId = GetReceiptFileId(msg);

                if (receiptFileId != null)
                {
                    await HandleReceiptPhotoAsync(botClient, msg, chatId, receiptFileId, ct);
                    return;
                }

                _logger.LogInformation(
                    "Receipt expected from {ChatId} but message had no photo/document (type {Type})",
                    chatId, msg.Type);

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

                // The name was requested on the way to sending a receipt.
                if (state.PendingReceipt)
                {
                    state.PendingReceipt = false;
                    state.Step = ConversationStep.AwaitingReceiptPhoto;

                    await botClient.SendMessage(
                        chatId,
                        "ممنون! حالا لطفاً عکس رسید پرداخت خود را ارسال کنید.\n" +
                        "در صورت نیاز می‌توانید توضیحات را در caption بنویسید.",
                        cancellationToken: ct);

                    return;
                }

                var item = await _menu.GetByKeyAsync(state.DrinkType);

                if (item == null)
                {
                    await SendUnavailableItemAsync(botClient, chatId, ct);
                    return;
                }

                await PromptAfterDrinkSelectedAsync(
                    botClient, chatId, state, item, isNameJustEntered: true, ct);

                return;
            }

            await botClient.SendMessage(
                chatId,
                "برای باز کردن منو /start را ارسال کنید.");
        }

        // Returns the file id of a receipt image whether it arrived as a compressed
        // photo or as a document/file (Bale often sends images as documents).
        private static string? GetReceiptFileId(Message msg)
        {
            if (msg.Photo is { Length: > 0 })
                return msg.Photo.OrderByDescending(p => p.FileSize).First().FileId;

            if (msg.Document != null)
                return msg.Document.FileId;

            return null;
        }

        private async Task HandleReceiptPhotoAsync(
            ITelegramBotClient botClient,
            Message msg,
            long chatId,
            string fileId,
            CancellationToken ct)
        {
            var user = await _users.GetUserByChatIdAsync(chatId);
            var displayName = user?.DisplayName ?? msg.Chat.Username ?? chatId.ToString();

            var receipt = new PaymentReceipt
            {
                ChatId = chatId,
                DisplayName = displayName,
                TelegramFileId = fileId,
                UserCaption = msg.Caption
            };

            var receiptId = await _accounts.CreateReceiptAsync(receipt);

            await _receiptFiles.SaveTelegramPhotoToDatabaseAsync(
                botClient,
                fileId,
                receiptId,
                ct);

            _states.Remove(chatId);

            await botClient.SendMessage(
                chatId,
                "رسید پرداخت شما دریافت شد.\n" +
                "مدیر به زودی آن را بررسی و حساب شما را شارژ می‌کند.");

            await NotifyAdminReceiptAsync(botClient, receiptId, displayName, chatId, fileId, msg.Caption);
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
                // Start of a new ordering session: reset the running totals.
                var state = _states.GetOrCreate(chatId);
                state.SessionItemCount = 0;
                state.SessionTotalToman = 0;

                await ShowDrinkMenuAsync(botClient, chatId, "خیلی ام عالی دوست من! خوشحالیم اینجایی چی میل داری؟", ct);
                return;
            }

            if (data == "order_more")
            {
                await ShowDrinkMenuAsync(botClient, chatId, "بسیار عالی! آیتم بعدی را انتخاب کنید:", ct);
                return;
            }

            if (data == "order_done")
            {
                await FinishOrderSessionAsync(botClient, chatId, ct);
                return;
            }

            if (data == "menu_receipt")
            {
                var state = _states.GetOrCreate(chatId);
                state.DrinkType = string.Empty;
                state.ShotCount = 0;

                var receiptUser = await _users.GetUserByChatIdAsync(chatId);

                // New user without a name: ask for the full name first, then continue to the receipt.
                if (receiptUser == null || string.IsNullOrWhiteSpace(receiptUser.DisplayName))
                {
                    state.Step = ConversationStep.Name;
                    state.PendingReceipt = true;

                    await botClient.SendMessage(
                        chatId,
                        "خوش آمدید! لطفاً نام خود را وارد کنید (برای شناسایی رسید پرداخت):",
                        cancellationToken: ct);

                    return;
                }

                state.Step = ConversationStep.AwaitingReceiptPhoto;
                state.PendingReceipt = false;

                await botClient.SendMessage(
                    chatId,
                    "لطفاً عکس رسید پرداخت خود را ارسال کنید.\n" +
                    "در صورت نیاز می‌توانید توضیحات را در caption بنویسید.",
                    cancellationToken: ct);

                return;
            }

            if (data is "shots_1" or "shots_2")
            {
                if (!_states.TryGet(chatId, out var state) ||
                    string.IsNullOrEmpty(state!.DrinkType))
                {
                    await SendRestartHintAsync(botClient, chatId, ct);
                    return;
                }

                var item = await _menu.GetByKeyAsync(state.DrinkType);

                if (item == null)
                {
                    await SendUnavailableItemAsync(botClient, chatId, ct);
                    return;
                }

                state.ShotCount = data == "shots_1" ? (byte)1 : (byte)2;

                await CompleteOrderAsync(botClient, chatId, state, item, ct);
                return;
            }

            // Otherwise treat the callback data as a menu item key.
            var selectedItem = await _menu.GetByKeyAsync(data);

            if (selectedItem is { IsActive: true })
            {
                var user = await _users.GetUserByChatIdAsync(chatId);
                var isSubscriber = user?.IsSubscriber ?? false;

                if (!IsVisibleTo(selectedItem, isSubscriber))
                {
                    await SendUnavailableItemAsync(botClient, chatId, ct);
                    return;
                }

                var state = _states.GetOrCreate(chatId);
                state.DrinkType = selectedItem.ItemKey;
                state.ShotCount = 0;
                state.Step = ConversationStep.None;

                if (user == null || string.IsNullOrWhiteSpace(user.DisplayName))
                {
                    state.Step = ConversationStep.Name;

                    await botClient.SendMessage(
                        chatId,
                        "خوش آمدید! لطفاً نام خود را وارد کنید (برای سفارش‌های بعدی شما را می‌شناسیم):",
                        cancellationToken: ct);

                    return;
                }

                state.DisplayName = user.DisplayName;

                await PromptAfterDrinkSelectedAsync(
                    botClient, chatId, state, selectedItem, isNameJustEntered: false, ct);
            }
        }

        // Decides the next step after a drink is chosen (and the user's name is known):
        // ask for quantity, ask about chocolate, or finalize the order directly.
        private async Task PromptAfterDrinkSelectedAsync(
            ITelegramBotClient botClient,
            long chatId,
            UserState state,
            MenuItem item,
            bool isNameJustEntered,
            CancellationToken ct)
        {
            if (item.SupportsShots)
            {
                await botClient.SendMessage(
                    chatId,
                    BuildQuantityPrompt(state.DisplayName, item, isNameJustEntered),
                    replyMarkup: BuildQuantityMenu(item.Unit),
                    cancellationToken: ct);

                return;
            }

            state.ShotCount = 1;

            await CompleteOrderAsync(botClient, chatId, state, item, ct);
        }

        private async Task CompleteOrderAsync(
            ITelegramBotClient botClient,
            long chatId,
            UserState state,
            MenuItem item,
            CancellationToken ct)
        {
            var displayName = state.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                var user = await _users.GetUserByChatIdAsync(chatId);
                displayName = user?.DisplayName ?? "نامشخص";
            }

            if (state.ShotCount == 0)
                state.ShotCount = 1;

            var price = await _prices.GetPriceAsync(
                item.ItemKey,
                state.ShotCount,
                withChocolate: false) ?? 0;

            var order = new CoffeeOrder
            {
                ChatId = chatId,
                DisplayName = displayName,
                DrinkType = item.ItemKey,
                ShotCount = state.ShotCount,
                WithChocolate = false,
                PriceInToman = price
            };

            var orderId = await _orders.SaveOrderAsync(order);

            await _accounts.AddDebitAsync(
                chatId,
                price,
                BuildDebitDescription(item, state.ShotCount),
                orderId);

            var confirmation = BuildOrderConfirmation(
                displayName,
                item,
                state.ShotCount,
                price);

            await botClient.SendMessage(
                chatId,
                confirmation,
                cancellationToken: ct);

            await NotifyAdminAsync(botClient, order, item);

            // Accumulate the running session totals and reset the per-item selection.
            state.SessionItemCount += 1;
            state.SessionTotalToman += price;
            state.DrinkType = string.Empty;
            state.ShotCount = 0;
            state.Step = ConversationStep.None;

            await botClient.SendMessage(
                chatId,
                "آیا مایل به انتخاب آیتم دیگری هستید؟",
                replyMarkup: BuildAddMoreMenu(),
                cancellationToken: ct);
        }

        private async Task ShowDrinkMenuAsync(
            ITelegramBotClient botClient,
            long chatId,
            string prompt,
            CancellationToken ct)
        {
            var user = await _users.GetUserByChatIdAsync(chatId);
            var menu = await _menu.GetActiveForSubscriberAsync(user?.IsSubscriber ?? false);

            if (menu.Count == 0)
            {
                await botClient.SendMessage(
                    chatId,
                    "در حال حاضر آیتمی در منو موجود نیست.",
                    cancellationToken: ct);

                return;
            }

            await botClient.SendMessage(
                chatId,
                prompt,
                replyMarkup: BuildDrinkMenu(menu),
                cancellationToken: ct);
        }

        private async Task FinishOrderSessionAsync(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken ct)
        {
            var count = 0;
            var total = 0;

            if (_states.TryGet(chatId, out var state) && state != null)
            {
                count = state.SessionItemCount;
                total = state.SessionTotalToman;
            }

            var message = count > 0
                ? $"سفارش شما تکمیل شد!\n\nتعداد آیتم: {count}\nجمع کل: {total:N0} تومان\n\nبرای منوی اصلی /start را ارسال کنید."
                : "سفارشی ثبت نشد.\n\nبرای منوی اصلی /start را ارسال کنید.";

            await botClient.SendMessage(chatId, message, cancellationToken: ct);

            _states.Remove(chatId);
        }

        private async Task SendRestartHintAsync(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken ct)
        {
            await botClient.SendMessage(
                chatId,
                "لطفاً /start را بزنید و «ثبت سفارش» را انتخاب کنید.",
                replyMarkup: BuildMainMenu(),
                cancellationToken: ct);
        }

        private async Task SendUnavailableItemAsync(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken ct)
        {
            _states.Remove(chatId);

            await botClient.SendMessage(
                chatId,
                "این آیتم دیگر در منو موجود نیست. لطفاً /start را بزنید و دوباره انتخاب کنید.",
                replyMarkup: BuildMainMenu(),
                cancellationToken: ct);
        }

        private static bool IsVisibleTo(MenuItem item, bool isSubscriber) =>
            item.Visibility == MenuVisibility.Both ||
            (item.Visibility == MenuVisibility.SubscribersOnly && isSubscriber) ||
            (item.Visibility == MenuVisibility.NonSubscribersOnly && !isSubscriber);

        private static string BuildQuantityPrompt(
            string displayName,
            MenuItem item,
            bool isNameJustEntered)
        {
            return isNameJustEntered
                ? $"{displayName} عزیز، ممنون! برای {item.NamePersian} چند {item.Unit} می‌خواهید؟"
                : $"{displayName} عزیز! برای {item.NamePersian} چند {item.Unit} می‌خواهید؟";
        }

        private static string BuildDebitDescription(
            MenuItem item,
            byte shotCount)
        {
            if (!item.SupportsShots)
                return $"سفارش: {item.NamePersian}";

            return $"سفارش: {item.NamePersian} {shotCount} {item.Unit}";
        }

        private static string BuildOrderConfirmation(
            string displayName,
            MenuItem item,
            byte shotCount,
            int price)
        {
            var lines = new List<string>
            {
                "سفارش شما ثبت شد!",
                "",
                $"نام: {displayName}",
                $"نوشیدنی: {item.NamePersian}"
            };

            if (item.SupportsShots)
            {
                lines.Add($"{item.Unit}: {shotCount}");
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
            MenuItem item)
        {
            var adminChatIdSetting = _configuration["BaleSettings:AdminBotId"];

            if (string.IsNullOrEmpty(adminChatIdSetting))
                return;

            try
            {
                var adminChatId = long.Parse(adminChatIdSetting);

                var quantityLine = item.SupportsShots
                    ? $"{item.Unit}: {order.ShotCount}\n"
                    : string.Empty;

                await botClient.SendMessage(
                    adminChatId,
                    "سفارش قهوه جدید\n\n" +
                    $"نام: {order.DisplayName}\n" +
                    $"شناسه چت: {order.ChatId}\n" +
                    $"نوشیدنی: {item.NamePersian}\n" +
                    quantityLine +
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

        private static InlineKeyboardMarkup BuildDrinkMenu(IReadOnlyList<MenuItem> items)
        {
            var rows = new List<InlineKeyboardButton[]>();

            // Two items per row.
            for (var i = 0; i < items.Count; i += 2)
            {
                var row = new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(items[i].NamePersian, items[i].ItemKey)
                };

                if (i + 1 < items.Count)
                {
                    row.Add(InlineKeyboardButton.WithCallbackData(
                        items[i + 1].NamePersian, items[i + 1].ItemKey));
                }

                rows.Add(row.ToArray());
            }

            return new InlineKeyboardMarkup(rows);
        }

        private static InlineKeyboardMarkup BuildAddMoreMenu()
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("بله، آیتم دیگر", "order_more")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("خیر، همین کافیه", "order_done")
                }
            });
        }

        private static InlineKeyboardMarkup BuildQuantityMenu(string unit)
        {
            return new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData($"۱ {unit}", "shots_1"),
                    InlineKeyboardButton.WithCallbackData($"۲ {unit}", "shots_2")
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
