namespace BaleManagerSystem.Models.ViewModels
{
    public class UserChatIdViewModel
    {
        public long ChatId { get; set; }

        public string Username { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public DateTime FirstSeen { get; set; }
    }
}
