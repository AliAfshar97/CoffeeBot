using BaleManagerSystem.Models;

namespace BaleManagerSystem.Models.ViewModels
{
    public class AccountBalanceSummary
    {
        public long ChatId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public int TotalDebit { get; set; }

        public int TotalCredit { get; set; }

        public int Remaining => TotalDebit - TotalCredit;

        public List<AccountLedgerEntry> Transactions { get; set; } = new();
    }
}
