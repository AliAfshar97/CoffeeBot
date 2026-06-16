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

        public DateTime CreatedAt { get; set; }
    }
}
