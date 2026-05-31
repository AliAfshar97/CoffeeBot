using BaleManagerSystem.Models;
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
        string text,
        string fileId)
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
                        file_id = fileId
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


    public async Task<bool> SendPhotoAsync(
        long chatId,
        string text,
        string fileId)
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
                chat_id = chatId,

                from_chat_id = botId,

                photo = fileId
            };

            var json =
                JsonSerializer.Serialize(body);

            var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    $@"https://tapi.bale.ai/bot{_config["BaleSettings:Token"]}/sendPhoto");

            //request.Headers.Add(
            //    "api-access-key",
            //    apiKey);

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


    public async Task<string?> UploadFileAsync(
        IFormFile file)
    {
        using var client = new HttpClient();

        var _apiKey =
                _config["BaleSettings:ApiKey"];

        client.DefaultRequestHeaders.Add(
            "api-access-key",
            _apiKey);

        using var form =
            new MultipartFormDataContent();

        using var stream =
            file.OpenReadStream();

        form.Add(
            new StreamContent(stream),
            "file",
            file.FileName);

        var response =
            await client.PostAsync(
                "https://safir.bale.ai/api/v3/upload_file",
                form);

        if (!response.IsSuccessStatusCode)
            return null;

        var result =
            await response.Content
                .ReadFromJsonAsync<UploadFileResponse>();

        return result?.FileId;
    }
}