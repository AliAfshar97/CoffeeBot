using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class BaleMessageService
{
    private readonly HttpClient _httpClient;

    private readonly IConfiguration _config;

    public BaleMessageService(
        HttpClient httpClient,
        IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<bool> SendMessageAsync(
        string phoneNumber,
        string text)
    {
        try
        {
            var apiKey =
                _config["BaleSettings:ApiKey"];

            var botId =
                Convert.ToInt32(
                    _config["BaleSettings:BotId"]);

            var body = new
            {
                request_id = Guid.NewGuid().ToString(),

                bot_id = botId,

                phone_number = phoneNumber,

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
                                        text = "Website",
                                        url = "https://example.com"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var json =
                JsonSerializer.Serialize(body);

            var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://safir.bale.ai/api/v3/send_message");

            request.Headers.Add(
                "api-access-key",
                apiKey);

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var response =
                await _httpClient.SendAsync(request);

            var result =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(result);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }
}