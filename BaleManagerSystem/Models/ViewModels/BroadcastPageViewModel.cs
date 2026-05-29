namespace BaleManagerSystem.Models.ViewModels
{
    public class BroadcastPageViewModel
    {
        public string Message { get; set; } = "";

        public List<string> SelectedPhones
        { get; set; }
            = new();

        public List<UserModel> Users
        { get; set; }
            = new();
    }
}
