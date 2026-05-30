namespace BaleManagerSystem.Models.ViewModels
{
    public class BroadcastPageViewModel
    {
        public string Message { get; set; } = "";

        public string RecipientType { get; set; }
            = "Phone";

        public List<string> SelectedPhones { get; set; }
            = new();

        public List<long> SelectedChatIds { get; set; }
            = new();

        public IFormFile? Attachment { get; set; }

        public List<UserModel> PhoneUsers { get; set; }
            = new();

        public List<UserChatIdViewModel> TelegramUsers { get; set; }
            = new();
    }
}
