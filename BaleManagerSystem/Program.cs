using BaleManagerSystem.HostedServices;
using BaleManagerSystem.Services;
using Microsoft.OpenApi.Models;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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
    var token = builder.Configuration["BaleSettings:Token"]!;

    return new TelegramBotClient(token);
});

builder.Services.AddSingleton<UserStateService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<SafirUserRepository>();
builder.Services.AddHttpClient<BaleMessageService>();

builder.Services.AddScoped<IConsultationRepository,
    ConsultationRepository>();

builder.Services.AddScoped<BaleUpdateHandler>();

builder.Services.AddScoped<BroadcastService>();

builder.Services.AddHostedService<BalePollingService>();

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();