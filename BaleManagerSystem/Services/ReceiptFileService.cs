using BaleManagerSystem.Models;
using Telegram.Bot;

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

        public async Task<bool> SaveTelegramPhotoToDatabaseAsync(
            ITelegramBotClient botClient,
            string fileId,
            int receiptId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await botClient.GetFile(fileId, cancellationToken);

                if (string.IsNullOrEmpty(file.FilePath))
                    return false;

                await using var stream = new MemoryStream();
                await botClient.DownloadFile(file.FilePath, stream, cancellationToken);

                var bytes = stream.ToArray();

                if (bytes.Length == 0)
                    return false;

                var contentType = GuessContentType(file.FilePath);
                await _accounts.UpdateReceiptImageAsync(receiptId, bytes, contentType);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save receipt photo to database for receipt {ReceiptId}", receiptId);
                return false;
            }
        }

        public async Task<(byte[]? Content, string ContentType)> GetReceiptImageAsync(
            ITelegramBotClient botClient,
            PaymentReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            if (receipt.ImageContent is { Length: > 0 })
            {
                return (receipt.ImageContent, receipt.ImageContentType ?? "image/jpeg");
            }

            var fileBytes = await TryReadLegacyFileAsync(receipt, cancellationToken);

            if (fileBytes != null)
                return (fileBytes, "image/jpeg");

            if (string.IsNullOrEmpty(receipt.TelegramFileId))
                return (null, "image/jpeg");

            try
            {
                var file = await botClient.GetFile(receipt.TelegramFileId, cancellationToken);

                if (string.IsNullOrEmpty(file.FilePath))
                    return (null, "image/jpeg");

                await using var stream = new MemoryStream();
                await botClient.DownloadFile(file.FilePath, stream, cancellationToken);

                var bytes = stream.ToArray();

                if (bytes.Length == 0)
                    return (null, "image/jpeg");

                await TrySaveLegacyFileAsync(receipt, bytes, cancellationToken);

                return (bytes, GuessContentType(file.FilePath));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load receipt image for receipt {ReceiptId}", receipt.Id);
                return (null, "image/jpeg");
            }
        }

        private async Task<byte[]?> TryReadLegacyFileAsync(
            PaymentReceipt receipt,
            CancellationToken cancellationToken)
        {
            var fullPath = GetFullPath(receipt.Id);

            if (File.Exists(fullPath))
                return await File.ReadAllBytesAsync(fullPath, cancellationToken);

            if (string.IsNullOrWhiteSpace(receipt.LocalFilePath))
                return null;

            var relativePath = receipt.LocalFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var legacyPath = Path.Combine(_environment.WebRootPath, relativePath);

            if (File.Exists(legacyPath))
                return await File.ReadAllBytesAsync(legacyPath, cancellationToken);

            return null;
        }

        private async Task TrySaveLegacyFileAsync(
            PaymentReceipt receipt,
            byte[] bytes,
            CancellationToken cancellationToken)
        {
            try
            {
                EnsureReceiptsDirectory();

                var fullPath = GetFullPath(receipt.Id);
                await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

                var relativePath = GetRelativePath(receipt.Id);

                if (string.IsNullOrEmpty(receipt.LocalFilePath))
                {
                    await _accounts.UpdateReceiptFilePathAsync(receipt.Id, relativePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache legacy receipt file for receipt {ReceiptId}", receipt.Id);
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

        private static string GuessContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "image/jpeg"
            };
        }
    }
}
