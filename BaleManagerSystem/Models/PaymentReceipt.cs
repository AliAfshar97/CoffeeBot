namespace BaleManagerSystem.Models
{
    public class PaymentReceipt
    {
        public int Id { get; set; }

        public long ChatId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string TelegramFileId { get; set; } = string.Empty;

        public string? LocalFilePath { get; set; }

        public string? UserCaption { get; set; }

        public string Status { get; set; } = ReceiptStatuses.Pending;

        public int? CreditAmount { get; set; }

        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }
    }
}
