using BaleManagerSystem.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace BaleManagerSystem.HostedServices
{
    public class BalePollingService : BackgroundService
    {
        private readonly ITelegramBotClient _botClient;

        private readonly IServiceScopeFactory _scopeFactory;

        public BalePollingService(
            ITelegramBotClient botClient,
            IServiceScopeFactory scopeFactory)
        {
            _botClient = botClient;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            ReceiverOptions options = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                options,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Telegram.Bot.Types.Update update,
            CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var handler =
                scope.ServiceProvider
                    .GetRequiredService<BaleUpdateHandler>();

            await handler.HandleUpdateAsync(
                botClient,
                update,
                ct);
        }

        private async Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();

            var handler =
                scope.ServiceProvider
                    .GetRequiredService<BaleUpdateHandler>();

            await handler.HandleErrorAsync(
                botClient,
                exception,
                ct);
        }
    }
}
