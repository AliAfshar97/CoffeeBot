namespace BaleManagerSystem.Models
{
    public class ChatUser
    {
        public long ChatId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string? DisplayName { get; set; }

        public bool IsSubscriber { get; set; }

        public DateTime FirstSeen { get; set; }
    }
}
