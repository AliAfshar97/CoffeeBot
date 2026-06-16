using BaleManagerSystem.Models;

namespace BaleManagerSystem.Models.ViewModels
{
    public class PersonPaymentSummary
    {
        public long ChatId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public int OrderCount { get; set; }

        public int TotalToman { get; set; }

        public List<CoffeeOrder> Orders { get; set; } = new();
    }
}
