namespace BaleManagerSystem.Models.ViewModels
{
    public class AddCreditViewModel
    {
        public long ChatId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public int Amount { get; set; }

        public string? Description { get; set; }
    }
}
