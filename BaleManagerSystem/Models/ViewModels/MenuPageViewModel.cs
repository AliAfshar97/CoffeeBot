using BaleManagerSystem.Models;

namespace BaleManagerSystem.Models.ViewModels
{
    public class MenuPageViewModel
    {
        public List<MenuItem> Items { get; set; } = new();

        public MenuItemFormViewModel Form { get; set; } = new();

        // True when the form is editing an existing item rather than adding one.
        public bool IsEditing { get; set; }
    }
}
