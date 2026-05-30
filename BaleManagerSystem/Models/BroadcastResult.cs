namespace BaleManagerSystem.Models
{
    public class BroadcastResult
    {
        public int SuccessCount { get; set; }

        public int FailedCount { get; set; }

        public List<long> FailedChatIds { get; set; }
            = new();
    }
}
