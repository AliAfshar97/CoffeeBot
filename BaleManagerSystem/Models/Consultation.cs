namespace BaleManagerSystem.Models
{
    public class Consultation
    {
        public long ChatId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Company { get; set; } = string.Empty;

        public string ShortBrief { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }
}
