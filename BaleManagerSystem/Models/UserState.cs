namespace BaleManagerSystem.Models
{
    public class UserState
    {
        public ConversationStep Step { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string DrinkType { get; set; } = string.Empty;

        public byte ShotCount { get; set; }

        // True when the user is entering their name on the way to sending a receipt
        // (rather than placing an order).
        public bool PendingReceipt { get; set; }

        // Running totals for the current ordering session (multiple items in one go).
        public int SessionItemCount { get; set; }

        public int SessionTotalToman { get; set; }
    }
}
