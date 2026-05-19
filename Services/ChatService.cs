using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AICharacterMaker.Models;

namespace AICharacterMaker.Services
{
    public class ChatService
    {
        private readonly HttpClient _http;
        private readonly string _model;

        public ChatService(string apiKey, string model = "gpt-4o-mini")
        {
            _model = model;
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<(string emotion, string text)?> SendMessageAsync(
            Character character,
            List<Message> history,
            string userMessage)
        {
            var systemPrompt = $"""
                あなたは「{character.Name}」というキャラクターです。
                性格: {character.Personality}

                ユーザーと会話してください。返答は必ず以下のJSON形式のみで返してください:
                {{"emotion": "happy|sad|angry|surprised|neutral|relaxed", "text": "返答テキスト"}}
                """;

            var messages = new List<object> { new { role = "system", content = systemPrompt } };
            foreach (var msg in history.TakeLast(20))
                messages.Add(new { role = msg.Role, content = msg.Text });
            messages.Add(new { role = "user", content = userMessage });

            var payload = new
            {
                model = _model,
                messages,
                response_format = new { type = "json_object" },
                max_tokens = 500
            };

            var response = await _http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", payload);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var content = json
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            var result = JsonSerializer.Deserialize<JsonElement>(content);
            var emotion = result.TryGetProperty("emotion", out var e) ? e.GetString() ?? "neutral" : "neutral";
            var text = result.TryGetProperty("text", out var t) ? t.GetString() ?? string.Empty : string.Empty;

            return (emotion, text);
        }
    }
}
