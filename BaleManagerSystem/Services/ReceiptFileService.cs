using BaleManagerSystem.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BaleManagerSystem.Services
{
    public class ReceiptFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IAccountRepository _accounts;
        private readonly ILogger<ReceiptFileService> _logger;

        public ReceiptFileService(
            IWebHostEnvironment environment,
            IAccountRepository accounts,
            ILogger<ReceiptFileService> logger)
        {
            _environment = environment;
            _accounts = accounts;
            _logger = logger;
        }

        public async Task<string?> SaveTelegramPhotoAsync(
            ITelegramBotClient botClient,
            string fileId,
            int receiptId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                EnsureReceiptsDirectory();

                var file = await botClient.GetFile(fileId, cancellationToken);

                if (string.IsNullOrEmpty(file.FilePath))
                    return null;

                var relativePath = GetRelativePath(receiptId);
                var fullPath = GetFullPath(receiptId);

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

        public async Task<byte[]?> GetReceiptImageAsync(
            ITelegramBotClient botClient,
            PaymentReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            EnsureReceiptsDirectory();

            var fullPath = GetFullPath(receipt.Id);

            if (File.Exists(fullPath))
            {
                return await File.ReadAllBytesAsync(fullPath, cancellationToken);
            }

            if (string.IsNullOrEmpty(receipt.TelegramFileId))
                return null;

            try
            {
                var file = await botClient.GetFile(receipt.TelegramFileId, cancellationToken);

                if (string.IsNullOrEmpty(file.FilePath))
                    return null;

                await using var stream = new MemoryStream();
                await botClient.DownloadFile(file.FilePath, stream, cancellationToken);

                var bytes = stream.ToArray();
                await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

                var relativePath = GetRelativePath(receipt.Id);
                if (string.IsNullOrEmpty(receipt.LocalFilePath))
                {
                    await _accounts.UpdateReceiptFilePathAsync(receipt.Id, relativePath);
                }

                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load receipt image for receipt {ReceiptId}", receipt.Id);
                return null;
            }
        }

        private void EnsureReceiptsDirectory()
        {
            var directory = Path.Combine(_environment.WebRootPath, "receipts");
            Directory.CreateDirectory(directory);
        }

        private static string GetRelativePath(int receiptId) => $"/receipts/{receiptId}.jpg";

        private string GetFullPath(int receiptId) =>
            Path.Combine(_environment.WebRootPath, "receipts", $"{receiptId}.jpg");
    }
}
