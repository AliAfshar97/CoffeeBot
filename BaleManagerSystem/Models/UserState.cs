namespace BaleManagerSystem.Models
{
    public class UserState
    {
        public ConversationStep Step { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string DrinkType { get; set; } = string.Empty;

        public byte ShotCount { get; set; }
    }
}
