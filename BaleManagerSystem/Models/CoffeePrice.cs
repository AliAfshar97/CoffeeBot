namespace BaleManagerSystem.Models
{
    public class CoffeePrice
    {
        public int Id { get; set; }

        public string DrinkType { get; set; } = string.Empty;

        public byte ShotCount { get; set; }

        public bool WithChocolate { get; set; }

        public int PriceInToman { get; set; }
    }
}
