namespace BaleManagerSystem.Models
{
    public class BroadcastRequest
    {
        public string Message { get; set; } = "";

        public List<long> ChatIds { get; set; }
            = new();
    }
}
