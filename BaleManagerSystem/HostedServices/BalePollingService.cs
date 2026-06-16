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

        private readonly ILogger<BalePollingService> _logger;

        public BalePollingService(
            ITelegramBotClient botClient,
            IServiceScopeFactory scopeFactory,
            ILogger<BalePollingService> logger)
        {
            _botClient = botClient;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            try
            {
                var me = await _botClient.GetMe(stoppingToken);
                _logger.LogInformation(
                    "Bale bot connected: @{Username} (id {Id})",
                    me.Username,
                    me.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Cannot connect to Bale bot API. Check BaleSettings:Token and ApiBaseUrl.");
            }

            ReceiverOptions options = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                options,
                cancellationToken: stoppingToken);

            _logger.LogInformation("Bale bot polling started.");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Telegram.Bot.Types.Update update,
            CancellationToken ct)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in Bale update {UpdateId}", update.Id);
            }
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
