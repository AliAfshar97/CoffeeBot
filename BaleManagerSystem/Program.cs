using BaleManagerSystem.HostedServices;
using BaleManagerSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi.Models;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Telegram Bot API",
        Version = "v1"
    });
});

builder.Services.AddSingleton<ITelegramBotClient>(x =>
{
    var configuration = x.GetRequiredService<IConfiguration>();
    var token = configuration["BaleSettings:Token"]!;
    var baseUrl = configuration["BaleSettings:ApiBaseUrl"] ?? "https://tapi.bale.ai";

    return new TelegramBotClient(new TelegramBotClientOptions(token, baseUrl));
});

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        options.AccessDeniedPath =
            "/Account/Login";

        options.ExpireTimeSpan =
            TimeSpan.FromDays(7);
    });

builder.Services.AddSingleton<UserStateService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<SafirUserRepository>();

builder.Services.AddHttpClient<BaleMessageService>();

builder.Services.AddScoped<IConsultationRepository,
    ConsultationRepository>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<ICoffeePriceRepository, CoffeePriceRepository>();

builder.Services.AddScoped<PaymentReportExcelExporter>();

builder.Services.AddScoped<IAccountRepository, AccountRepository>();

builder.Services.AddScoped<AccountBalancesExcelExporter>();

builder.Services.AddScoped<ReceiptFileService>();

builder.Services.AddScoped<BaleUpdateHandler>();

builder.Services.AddScoped<BroadcastService>();

builder.Services.AddHostedService<BalePollingService>();

var app = builder.Build();


// IMPORTANT
// ONLY enable swagger in development

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var receiptsDirectory = Path.Combine(app.Environment.WebRootPath, "receipts");
Directory.CreateDirectory(receiptsDirectory);

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// API Controllers
app.MapControllers();


// MVC Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Admin}/{action=Dashboard}/{id?}");

app.Run();