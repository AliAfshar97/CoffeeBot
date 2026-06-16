namespace BaleManagerSystem.Models.ViewModels
{
    public class PaymentReportViewModel
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public List<PersonPaymentSummary> Summaries { get; set; } = new();

        public int GrandTotalToman { get; set; }

        public int TotalOrders { get; set; }
    }
}
