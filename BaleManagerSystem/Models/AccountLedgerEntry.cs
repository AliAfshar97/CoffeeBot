namespace BaleManagerSystem.Models
{
    public class AccountLedgerEntry
    {
        public int Id { get; set; }

        public long ChatId { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public int Amount { get; set; }

        public string? Description { get; set; }

        public int? OrderId { get; set; }

        public int? ReceiptId { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
