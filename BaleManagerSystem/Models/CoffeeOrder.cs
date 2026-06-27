namespace BaleManagerSystem.Models
{
    public class CoffeeOrder
    {
        public int Id { get; set; }

        public long ChatId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string DrinkType { get; set; } = string.Empty;

        public byte ShotCount { get; set; }

        public bool WithChocolate { get; set; }

        public int PriceInToman { get; set; }

        public DateTime CreatedAt { get; set; }

        // Display-only fields resolved from MenuItems when reading orders.
        // Null when the order's item no longer exists in the menu.
        public string? DrinkNamePersian { get; set; }

        public string? Unit { get; set; }

        public bool HasShots { get; set; }
    }
}
