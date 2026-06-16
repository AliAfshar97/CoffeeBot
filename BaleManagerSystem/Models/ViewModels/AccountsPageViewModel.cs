namespace BaleManagerSystem.Models.ViewModels
{
    public class AccountsPageViewModel
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public List<AccountBalanceSummary> Balances { get; set; } = new();

        public int GrandTotalDebit { get; set; }

        public int GrandTotalCredit { get; set; }

        public int GrandTotalRemaining { get; set; }
    }
}
