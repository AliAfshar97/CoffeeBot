using Telegram.Bot;
using Telegram.Bot.Types;

namespace BaleManagerSystem.Services
{
    public class ReceiptFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ReceiptFileService> _logger;

        public ReceiptFileService(
            IWebHostEnvironment environment,
            ILogger<ReceiptFileService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string?> SaveTelegramPhotoAsync(
            ITelegramBotClient botClient,
            PhotoSize photo,
            int receiptId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var directory = Path.Combine(_environment.WebRootPath, "receipts");
                Directory.CreateDirectory(directory);

                var relativePath = $"/receipts/{receiptId}.jpg";
                var fullPath = Path.Combine(directory, $"{receiptId}.jpg");

                var file = await botClient.GetFile(photo.FileId, cancellationToken);

                if (string.IsNullOrEmpty(file.FilePath))
                    return null;

                await using var stream = File.Create(fullPath);
                await botClient.DownloadFile(file.FilePath, stream, cancellationToken);

                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save receipt photo for receipt {ReceiptId}", receiptId);
                return null;
            }
        }
    }
}
