using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class BaleMessageService
{
    private readonly HttpClient _httpClient;

    // Your API Information
    private const string ApiUrl = "https://safir.bale.ai/api/v3/send_message";
    private const string ApiAccessKey = "K6RklrhGcjtLZoSk";
    private const int BotId = 1210996085;

    public BaleMessageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendBulkMessageAsync(List<string> phoneNumbers, string text)
    {
        foreach (var phone in phoneNumbers)
        {
            var requestBody = new
            {
                request_id = Guid.NewGuid().ToString(),

                bot_id = BotId,

                phone_number = phone,

                message_data = new
                {
                    message = new
                    {
                        text = text,

                        reply_markup = new
                        {
                            inline_keyboard = new object[]
                            {
                                new object[]
                                {
                                    new
                                    {
                                        text = "Open Website",
                                        url = "https://example.com"
                                    },

                                    new
                                    {
                                        text = "Copy Code",
                                        copy_text = "123456"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);

            request.Headers.Add("api-access-key", ApiAccessKey);

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);

            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Phone: {phone}");
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine(responseContent);
            Console.WriteLine("--------------------------------");
        }
    }
}