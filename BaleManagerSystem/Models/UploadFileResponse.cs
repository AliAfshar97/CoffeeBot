using System.Text.Json.Serialization;

namespace BaleManagerSystem.Models
{
    public class UploadFileResponse
    {
        [JsonPropertyName("file_id")]
        public string FileId { get; set; } = "";
    }
}
