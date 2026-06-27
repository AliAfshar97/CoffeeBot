using BaleManagerSystem.Models;

namespace BaleManagerSystem.Models.ViewModels
{
    public class EditOrderViewModel
    {
        public int Id { get; set; }

        public long ChatId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string DrinkType { get; set; } = string.Empty;

        public byte ShotCount { get; set; }

        public int PriceInToman { get; set; }

        public DateTime CreatedAt { get; set; }

        // Menu items available to pick from (populated for the dropdown).
        public List<MenuItem> MenuItems { get; set; } = new();
    }
}
