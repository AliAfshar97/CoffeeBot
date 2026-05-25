using BaleManagerSystem.Models;
using BaleManagerSystem.Services;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var bot = new TelegramBotClient("1210996085:oSqSqrpAEenM6oMs0QxSnOD6STA5nGm1JaI");

const string ConnectionString =
    "Server=192.168.7.101;Database=SaleBotManagerDB;User ID=sa;Password=;Encrypt=True;TrustServerCertificate=True;";


using CancellationTokenSource cts = new();

ConcurrentDictionary<long, UserState> userStates = new();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, cts.Token);

var me = await bot.GetMe();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
{
    long AdminChatId = 407302762;

    if (update.Message is not null)
    {
        var msg = update.Message;
        var chatId = msg.Chat.Id;
        var text = msg.Text ?? "";

        await SaveUser(chatId, msg.Chat.Username);

        // ================= /START =================
        if (text == "/start")
        {
            var mainMenu = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("تهیه نرم‌افزار ERP سازمانی", "erp") },
                new[] { InlineKeyboardButton.WithCallbackData("مهاجرت از سیستم ویندوزی به تحت وب", "web") },
                new[] { InlineKeyboardButton.WithCallbackData("خدمات شبکه و امنیت اطلاعات", "network") },
                new[] { InlineKeyboardButton.WithCallbackData("خدمات مالی و مالیاتی", "finance") },
                new[] { InlineKeyboardButton.WithCallbackData("سایر موارد", "other") }
            });

            await botClient.SendMessage(chatId,
                "موضوع مورد نظر را انتخاب کنید:",
                replyMarkup: mainMenu);

            return;
        }

        // ================= FOR    M FLOW =================
        if (userStates.TryGetValue(chatId, out var state))
        {
            switch (state.Step)
            {
                case "name":
                    state.FullName = text;
                    state.Step = "phone";
                    await botClient.SendMessage(chatId, "لطفا شماره تماس خود را وارد کنید.");
                    break;

                case "phone":
                    state.Phone = text;
                    state.Step = "company";
                    await botClient.SendMessage(chatId, "نام شرکت خود را وارد کنید.");
                    break;

                case "company":
                    state.Company = text;

                    // SAVE SQL
                    await SaveConsultation(chatId, state);

                    // ADMIN NOTIFY
                    await botClient.SendMessage(AdminChatId,
                        $"📥 NEW REQUEST\n\n" +
                        $"Name: {state.FullName}\n" +
                        $"Phone: {state.Phone}\n" +
                        $"Company: {state.Company}\n" +
                        $"Category: {state.Category}");

                    await botClient.SendMessage(chatId,
                        "درخواست شما ثبت شد ✅");

                    userStates.TryRemove(chatId, out _);
                    break;
            }
        }

        // ================= ADMIN BROADCAST =================
        if (chatId == AdminChatId && text.StartsWith("/broadcast "))
        {
            var message = text.Replace("/broadcast ", "");
            await Broadcast(botClient, message);
            await botClient.SendMessage(chatId, "Broadcast sent!");
        }
    }

    // ================= CALLBACK BUTTONS =================
    if (update.CallbackQuery is not null)
    {
        var cb = update.CallbackQuery;
        var chatId = cb.Message!.Chat.Id;
        var data = cb.Data;

        if (data is "erp" or "web" or "network" or "finance" or "other")
        {
            var category = data switch
            {
                "erp" => "ERP",
                "web" => "Web",
                "network" => "Network",
                "finance" => "Finance",
                _ => "Other"
            };

            var secondMenu = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("تماس با کارشناسان حساب رایان", "contact") },
                new[] { InlineKeyboardButton.WithCallbackData("ثبت درخواست مشاوره", "register") }
            });

            userStates[chatId] = new UserState
            {
                Category = category
            };

            await botClient.SendMessage(chatId,
                "یکی از گزینه‌ها را انتخاب کنید:",
                replyMarkup: secondMenu);

            await botClient.AnswerCallbackQuery(cb.Id);
        }

        if (data == "register")
        {
            userStates[chatId].Step = "name";
            await botClient.SendMessage(chatId, "نام و نام خانوادگی خود را وارد کنید.");
            await botClient.AnswerCallbackQuery(cb.Id);
        }

        if (data == "contact")
        {
            await botClient.SendMessage(chatId,
                "☎ 02187760\n📱 09101087760\n@hesabrayandm");

            await botClient.AnswerCallbackQuery(cb.Id);
        }
    }
}

async Task SaveUser(long chatId, string? username)
{
    using var conn = new SqlConnection(ConnectionString);
    await conn.OpenAsync();

    var cmd = new SqlCommand(@"
        IF NOT EXISTS (SELECT 1 FROM BotUserTransactions WHERE ChatId=@ChatId)
        INSERT INTO BotUserTransactions (ChatId, Username, FirstSeen)
        VALUES (@ChatId, @Username, GETDATE())
    ", conn);

    cmd.Parameters.AddWithValue("@ChatId", chatId);
    cmd.Parameters.AddWithValue("@Username", username ?? "");

    await cmd.ExecuteNonQueryAsync();
}


// ================= SQL: SAVE REQUEST =================
async Task SaveConsultation(long chatId, UserState s)
{
    using var conn = new SqlConnection(ConnectionString);
    await conn.OpenAsync();

    var cmd = new SqlCommand(@"
        INSERT INTO Consultations
        (ChatId, FullName, PhoneNumber, Company, Category, CreatedAt)
        VALUES
        (@ChatId, @FullName, @PhoneNumber, @Company, @Category, GETDATE())
    ", conn);

    cmd.Parameters.AddWithValue("@ChatId", chatId);
    cmd.Parameters.AddWithValue("@FullName", s.FullName);
    cmd.Parameters.AddWithValue("@PhoneNumber", s.Phone);
    cmd.Parameters.AddWithValue("@Company", s.Company);
    cmd.Parameters.AddWithValue("@Category", s.Category);

    await cmd.ExecuteNonQueryAsync();
}


// ================= BROADCAST =================
async Task Broadcast(ITelegramBotClient botClient, string message)
{
    using var conn = new SqlConnection(ConnectionString);
    await conn.OpenAsync();

    var cmd = new SqlCommand("SELECT ChatId FROM BotUserTransactions", conn);

    using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        try
        {
            await botClient.SendMessage(reader.GetInt64(0), message);
        }
        catch
        {
            // ignore blocked users
        }
    }
}


// ================= ERROR =================
Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken ct)
{
    Console.WriteLine(ex.Message);
    return Task.CompletedTask;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "Bale Broadcast API",
            Version = "v1"
        });
});

builder.Services.AddHttpClient<BaleMessageService>();

builder.Services.AddScoped<UserRepository>();

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();