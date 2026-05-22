namespace BaleManagerSystem.Models
{
    public class BotUser
    {
        public int Id { get; set; }

        public string PhoneNumber { get; set; } = "";

        public string? Username { get; set; }

        public DateTime FirstSeen { get; set; }
    }
}
