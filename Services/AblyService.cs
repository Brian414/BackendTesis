using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MyBackend.Services
{
    public class AblyService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AblyService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", 
                Convert.ToBase64String(Encoding.ASCII.GetBytes(_apiKey))
            );
        }

        public async Task SendMessageAsync(string channelName, object message)
        {
            var payload = new[] { new { name = "message", data = message } };
            var response = await _httpClient.PostAsJsonAsync(
                $"https://rest.ably.io/channels/{Uri.EscapeDataString(channelName)}/messages",
                payload
            );
            response.EnsureSuccessStatusCode();
        }
    }
}