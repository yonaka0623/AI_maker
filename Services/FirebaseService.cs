using AiCharacterMaker.Models;
using System.Text;
using System.Text.Json;

namespace AiCharacterMaker.Services;

public class FirebaseService
{
    const string ProjectId = "YOUR_FIREBASE_PROJECT_ID"; // Firebaseコンソールから取得
    const string BaseUrl = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents";

    readonly HttpClient _http = new();

    // ===== 認証トークン取得 =====
    async Task<string?> GetTokenAsync()
        => await SecureStorage.GetAsync("firebase_id_token");

    // ===== 現在のUID取得 =====
    public async Task<string?> GetCurrentUidAsync()
        => await SecureStorage.GetAsync("firebase_uid");

    // ===== ログアウト =====
    public Task SignOutAsync()
    {
        SecureStorage.Remove("firebase_id_token");
        SecureStorage.Remove("firebase_uid");
        return Task.CompletedTask;
    }

    // ===== Firestoreからドキュメント取得（共通） =====
    async Task<JsonElement?> GetDocumentAsync(string path)
    {
        var token = await GetTokenAsync();
        if (token == null) return null;

        var req = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/{path}");
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    // ===== selectedCharacterId取得 =====
    public async Task<string?> GetSelectedCharacterIdAsync(string uid)
    {
        var doc = await GetDocumentAsync($"users/{uid}");
        if (doc == null) return null;

        try
        {
            return doc.Value
                .GetProperty("fields")
                .GetProperty("selectedCharacterId")
                .GetProperty("stringValue")
                .GetString();
        }
        catch { return null; }
    }

    // ===== selectedCharacterId保存 =====
    public async Task SetSelectedCharacterAsync(string uid, string characterId)
    {
        var token = await GetTokenAsync();
        if (token == null) return;

        var url = $"{BaseUrl}/users/{uid}?updateMask.fieldPaths=selectedCharacterId";
        var body = JsonSerializer.Serialize(new
        {
            fields = new
            {
                selectedCharacterId = new { stringValue = characterId }
            }
        });

        var req = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _http.SendAsync(req);
    }

    // ===== キャラ1件取得 =====
    public async Task<Character?> GetCharacterAsync(string characterId)
    {
        var doc = await GetDocumentAsync($"characters/{characterId}");
        if (doc == null) return null;

        try
        {
            var fields = doc.Value.GetProperty("fields");
            return new Character
            {
                Id             = characterId,
                Name           = GetString(fields, "name"),
                Personality    = GetString(fields, "personality"),
                ShortPersonality = GetString(fields, "shortPersonality"),
                VoiceId        = GetString(fields, "voiceId"),
                IconUrl        = GetString(fields, "iconUrl"),
                ModelUrl       = GetString(fields, "modelUrl"),
            };
        }
        catch { return null; }
    }

    // ===== キャラ一覧取得 =====
    public async Task<List<Character>> GetCharactersAsync(string uid, string sortOrder)
    {
        var token = await GetTokenAsync();
        if (token == null) return new();

        var url = $"https://firestore.googleapis.com/v1/projects/{ProjectId}/databases/(default)/documents:runQuery";
        var body = JsonSerializer.Serialize(new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "characters" } },
                where = new
                {
                    fieldFilter = new
                    {
                        field = new { fieldPath = "creator" },
                        op = "EQUAL",
                        value = new { stringValue = uid }
                    }
                },
                orderBy = new[]
                {
                    new
                    {
                        field = new { fieldPath = "createdAt" },
                        direction = sortOrder == "asc" ? "ASCENDING" : "DESCENDING"
                    }
                }
            }
        });

        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return new();

        var json = await res.Content.ReadAsStringAsync();
        var docs = JsonSerializer.Deserialize<JsonElement[]>(json);
        if (docs == null) return new();

        var list = new List<Character>();
        foreach (var d in docs)
        {
            try
            {
                var docId = d.GetProperty("document")
                             .GetProperty("name").GetString()!
                             .Split('/').Last();
                var fields = d.GetProperty("document").GetProperty("fields");
                list.Add(new Character
                {
                    Id               = docId,
                    Name             = GetString(fields, "name"),
                    ShortPersonality = GetString(fields, "shortPersonality"),
                    IconUrl          = GetString(fields, "iconUrl"),
                });
            }
            catch { }
        }
        return list;
    }

    // ===== キャラ作成 =====
    public async Task CreateCharacterAsync(Dictionary<string, object?> data)
    {
        var token = await GetTokenAsync();
        if (token == null) return;

        var fields = new Dictionary<string, object>();
        foreach (var kv in data)
        {
            if (kv.Value == null)
                fields[kv.Key] = new { nullValue = (object?)null };
            else
                fields[kv.Key] = new { stringValue = kv.Value.ToString() };
        }
        // 作成日時
        fields["createdAt"] = new { timestampValue = DateTime.UtcNow.ToString("o") };

        var body = JsonSerializer.Serialize(new { fields });
        var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/characters")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _http.SendAsync(req);
    }

    // ===== スレッド保存 =====
    public async Task SaveThreadAsync(string threadId, string uid, string characterId)
    {
        var token = await GetTokenAsync();
        if (token == null) return;

        var url = $"{BaseUrl}/threads/{threadId}";
        var body = JsonSerializer.Serialize(new
        {
            fields = new
            {
                userId      = new { stringValue = uid },
                characterId = new { stringValue = characterId },
                updatedAt   = new { timestampValue = DateTime.UtcNow.ToString("o") }
            }
        });

        var req = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _http.SendAsync(req);
    }

    // ===== メッセージ追加 =====
    public async Task AddMessageAsync(string threadId, string role, string content)
    {
        var token = await GetTokenAsync();
        if (token == null) return;

        var url = $"{BaseUrl}/threads/{threadId}/messages";
        var body = JsonSerializer.Serialize(new
        {
            fields = new
            {
                role      = new { stringValue = role },
                content   = new { stringValue = content },
                createdAt = new { timestampValue = DateTime.UtcNow.ToString("o") }
            }
        });

        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _http.SendAsync(req);
    }

    // ===== メッセージ一覧取得 =====
    public async Task<List<Message>> GetMessagesAsync(string threadId)
    {
        var token = await GetTokenAsync();
        if (token == null) return new();

        var url = $"{BaseUrl}/threads/{threadId}/messages?orderBy=createdAt";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return new();

        var json = await res.Content.ReadAsStringAsync();
        var root = JsonSerializer.Deserialize<JsonElement>(json);

        var list = new List<Message>();
        if (!root.TryGetProperty("documents", out var docs)) return list;

        foreach (var d in docs.EnumerateArray())
        {
            try
            {
                var id = d.GetProperty("name").GetString()!.Split('/').Last();
                var fields = d.GetProperty("fields");
                list.Add(new Message
                {
                    Id   = id,
                    Role = GetString(fields, "role"),
                    Text = GetString(fields, "content"),
                });
            }
            catch { }
        }
        return list;
    }

    // ===== アイコンアップロード（Firebase Storage REST） =====
    public async Task<string?> UploadIconAsync(string uid, string localPath)
    {
        var token = await GetTokenAsync();
        if (token == null) return null;

        var fileName = $"users/{uid}/icon.jpg";
        var url = $"https://firebasestorage.googleapis.com/v0/b/{ProjectId}.appspot.com/o?name={Uri.EscapeDataString(fileName)}";

        var bytes = await File.ReadAllBytesAsync(localPath);
        var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(bytes)
        };
        req.Content.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        req.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);
        var storagePath = data.GetProperty("name").GetString();

        return $"https://firebasestorage.googleapis.com/v0/b/{ProjectId}.appspot.com/o/{Uri.EscapeDataString(storagePath!)}?alt=media";
    }

    // ===== ヘルパー：Firestoreのフィールドから文字列取得 =====
    static string GetString(JsonElement fields, string key)
    {
        try
        {
            return fields.GetProperty(key).GetProperty("stringValue").GetString() ?? "";
        }
        catch { return ""; }
    }
}