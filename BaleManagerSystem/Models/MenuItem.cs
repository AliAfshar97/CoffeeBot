namespace BaleManagerSystem.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        // Stable identifier used as Bale callback data and stored as CoffeeOrders.DrinkType
        public string ItemKey { get; set; } = string.Empty;

        public string NamePersian { get; set; } = string.Empty;

        // Whether the item offers the 1/2 quantity step
        public bool SupportsShots { get; set; }

        // Unit shown to the user for the quantity step, e.g. "شات" or "لیوان"
        public string Unit { get; set; } = "شات";

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
