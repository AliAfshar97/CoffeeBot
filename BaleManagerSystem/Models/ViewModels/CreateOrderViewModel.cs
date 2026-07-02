using BaleManagerSystem.Models;

namespace BaleManagerSystem.Models.ViewModels
{
    public class CreateOrderViewModel
    {
        public long ChatId { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string DrinkType { get; set; } = string.Empty;

        public byte ShotCount { get; set; } = 1;

        public int PriceInToman { get; set; }

        public List<MenuItem> MenuItems { get; set; } = new();

        public List<UserChatIdViewModel> Users { get; set; } = new();
    }
}
