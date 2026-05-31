namespace BaleManagerSystem.Models
{
    public class UserState
    {
        public string Category { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Company { get; set; } = string.Empty;

        public string ShortBrief { get; set; } = string.Empty;

        public ConversationStep Step { get; set; }
    }
}
