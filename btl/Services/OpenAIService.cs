using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BaiTapLon.Services
{
    public class OpenAIService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public OpenAIService()
        {
            _apiKey = ConfigurationManager.AppSettings["OpenAI_API_Key"];

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.openai.com/")
            };

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> AskAsync(string message)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return "⚠️ Chưa cấu hình API key OpenAI. Hãy thêm OpenAI_API_Key trong Web.config.";
            }

            string url = "v1/chat/completions";

            var payload = new
            {
                model = "gpt-4.1-mini",
                messages = new[]
                {
                    new { role = "system", content = "Bạn là trợ lý tư vấn sách cho website." },
                    new { role = "user", content = message }
                },
                max_tokens = 256
            };

            try
            {
                string jsonData = JObject.FromObject(payload).ToString();

                var content = new StringContent(
                    jsonData,
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    return $"⚠️ Lỗi OpenAI: {response.StatusCode}\n{err}";
                }

                var json = await response.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);

                var reply =
                    obj["choices"]?[0]?["message"]?["content"]?.ToString();

                return string.IsNullOrWhiteSpace(reply)
                    ? "🤖 Không nhận được nội dung trả lời."
                    : reply.Trim();
            }
            catch (Exception ex)
            {
                return "⚠️ Gọi OpenAI thất bại: " + ex.Message;
            }
        }
    }
}
