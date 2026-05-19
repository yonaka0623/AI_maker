using System.Text;
using System.Text.Json;

namespace AiCharacterMaker.Services;

public class ChatResponse
{
    public string Text { get; set; } = "";
    public string Emotion { get; set; } = "neutral";
}

public class ChatService
{
    static readonly HttpClient _http = new();

    public static async Task<ChatResponse?> SendAsync(
        string characterId,
        string personality,
        string userText,
        string threadId)
    {
        var apiKey = await SecureStorage.GetAsync("openai_api_key");
        if (string.IsNullOrEmpty(apiKey)) return null;

        var systemPrompt = string.IsNullOrEmpty(personality)
            ? "あなたは親切なAIキャラクターです。\n\n返答は必ず以下のJSON形式で返してください。\n{\"emotion\":\"happy|sad|angry|neutral\",\"text\":\"セリフ\"}"
            : $"あなたは次のキャラクターとして自然に会話してください。\n\n{personality}\n\n返答は必ず以下のJSON形式で返してください。\n{{\"emotion\":\"happy|sad|angry|neutral\",\"text\":\"セリフ\"}}";

        var body = JsonSerializer.Serialize(new
        {
            model = "gpt-4o-mini",
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userText }
            }
        });

        var req = new HttpRequestMessage(HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadAsStringAsync();
        var root = JsonSerializer.Deserialize<JsonElement>(json);

        var content = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";

        var parsed = JsonSerializer.Deserialize<JsonElement>(content);

        return new ChatResponse
        {
            Text    = parsed.GetProperty("text").GetString() ?? "",
            Emotion = parsed.GetProperty("emotion").GetString() ?? "neutral",
        };
    }
}