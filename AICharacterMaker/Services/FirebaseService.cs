using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AICharacterMaker.Models;

namespace AICharacterMaker.Services
{
    public class FirebaseService
    {
        private readonly HttpClient _http = new();
        private readonly string _projectId;
        private readonly string _storageBucket;
        private readonly string _baseUrl;

        public FirebaseService(string projectId, string storageBucket)
        {
            _projectId = projectId;
            _storageBucket = storageBucket;
            _baseUrl = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents";
        }

        public void SetAuthToken(string? idToken)
        {
            _http.DefaultRequestHeaders.Authorization = idToken != null
                ? new AuthenticationHeaderValue("Bearer", idToken)
                : null;
        }

        public async Task<List<Character>> GetCharactersAsync(string userId)
        {
            var queryUrl = $"{_baseUrl}:runQuery";
            var query = new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = "characters" } },
                    where = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = "creator" },   // userId → creator
                            op = "EQUAL",
                            value = new { stringValue = userId }
                        }
                    },
                    orderBy = new[] { new { field = new { fieldPath = "createdAt" }, direction = "DESCENDING" } }
                }
            };

            var response = await _http.PostAsJsonAsync(queryUrl, query);
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadFromJsonAsync<JsonElement[]>();
            if (json == null) return [];

            var characters = new List<Character>();
            foreach (var item in json)
            {
                if (!item.TryGetProperty("document", out var doc)) continue;
                characters.Add(ParseCharacter(doc));
            }
            return characters;
        }

        public async Task<string?> CreateCharacterAsync(Character character)
        {
            var url = $"{_baseUrl}/characters";
            var body = new
            {
                fields = new Dictionary<string, object>
                {
                    ["name"] = new { stringValue = character.Name },
                    ["shortDescription"] = new { stringValue = character.ShortDescription },
                    ["personality"] = new { stringValue = character.Personality },
                    ["vrmUrl"] = new { stringValue = character.VrmUrl },
                    ["iconUrl"] = new { stringValue = character.IconUrl },
                    ["ttsVoice"] = new { stringValue = character.TtsVoice },
                    ["creator"] = new { stringValue = character.Creator },
                    ["createdAt"] = new { timestampValue = character.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                }
            };

            var response = await _http.PostAsJsonAsync(url, body);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("name").GetString()!.Split('/').Last();
        }

        public async Task DeleteCharacterAsync(string characterId)
        {
            await _http.DeleteAsync($"{_baseUrl}/characters/{characterId}");
        }

        public async Task<List<Message>> GetMessagesAsync(string characterId)
        {
            var queryUrl = $"{_baseUrl}:runQuery";
            var query = new
            {
                structuredQuery = new
                {
                    from = new[] { new { collectionId = "messages" } },
                    where = new
                    {
                        fieldFilter = new
                        {
                            field = new { fieldPath = "characterId" },
                            op = "EQUAL",
                            value = new { stringValue = characterId }
                        }
                    },
                    orderBy = new[] { new { field = new { fieldPath = "timestamp" }, direction = "ASCENDING" } },
                    limit = 50
                }
            };

            var response = await _http.PostAsJsonAsync(queryUrl, query);
            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadFromJsonAsync<JsonElement[]>();
            if (json == null) return [];

            var messages = new List<Message>();
            foreach (var item in json)
            {
                if (!item.TryGetProperty("document", out var doc)) continue;
                messages.Add(ParseMessage(doc));
            }
            return messages;
        }

        public async Task SaveMessageAsync(Message message)
        {
            var body = new
            {
                fields = new Dictionary<string, object>
                {
                    ["characterId"] = new { stringValue = message.CharacterId },
                    ["role"] = new { stringValue = message.Role },
                    ["text"] = new { stringValue = message.Text },
                    ["emotion"] = new { stringValue = message.Emotion },
                    ["timestamp"] = new { timestampValue = message.Timestamp.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                }
            };
            await _http.PostAsJsonAsync($"{_baseUrl}/messages", body);
        }

        public async Task<string?> UploadVrmAsync(string userId, string fileName, Stream fileStream)
        {
            var objectPath = Uri.EscapeDataString($"vrm/{userId}/{fileName}");
            var url = $"https://firebasestorage.googleapis.com/v0/b/{_storageBucket}/o?uploadType=media&name={Uri.EscapeDataString($"vrm/{userId}/{fileName}")}";

            var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("model/gltf-binary");
            var response = await _http.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return null;

            return $"https://firebasestorage.googleapis.com/v0/b/{_storageBucket}/o/{objectPath}?alt=media";
        }

        private static Character ParseCharacter(JsonElement doc)
        {
            var fields = doc.GetProperty("fields");
            var name = doc.GetProperty("name").GetString()!;
            return new Character
            {
                Id = name.Split('/').Last(),
                Name = GetString(fields, "name"),
                ShortDescription = GetString(fields, "shortDescription"),
                Personality = GetString(fields, "personality"),
                VrmUrl = GetString(fields, "vrmUrl"),
                IconUrl = GetString(fields, "iconUrl"),
                TtsVoice = GetString(fields, "ttsVoice"),
                Creator = GetString(fields, "creator")
            };
        }

        private static Message ParseMessage(JsonElement doc)
        {
            var fields = doc.GetProperty("fields");
            return new Message
            {
                CharacterId = GetString(fields, "characterId"),
                Role = GetString(fields, "role"),
                Text = GetString(fields, "text"),
                Emotion = GetString(fields, "emotion")
            };
        }

        private static string GetString(JsonElement fields, string key)
        {
            if (fields.TryGetProperty(key, out var f) && f.TryGetProperty("stringValue", out var v))
                return v.GetString() ?? string.Empty;
            return string.Empty;
        }

        public async Task<string?> GetSelectedCharacterIdAsync(string userId)
        {
            var url = $"{_baseUrl}/users/{userId}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("fields", out var fields))
                return GetString(fields, "selectedCharacterId");
            return null;
        }

        public async Task SetSelectedCharacterAsync(string userId, string characterId)
        {
            var url = $"{_baseUrl}/users/{userId}?updateMask.fieldPaths=selectedCharacterId";
            var body = new
            {
                fields = new Dictionary<string, object>
                {
                    ["selectedCharacterId"] = new { stringValue = characterId }
                }
            };
            await _http.PatchAsJsonAsync(url, body);
        }

        public async Task<string?> UploadIconAsync(string userId, string fileName, Stream fileStream)
        {
            var objectPath = Uri.EscapeDataString($"icons/{userId}/{fileName}");
            var url = $"https://firebasestorage.googleapis.com/v0/b/{_storageBucket}/o?uploadType=media&name={Uri.EscapeDataString($"icons/{userId}/{fileName}")}";

            var content = new StreamContent(fileStream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            var response = await _http.PostAsync(url, content);
            if (!response.IsSuccessStatusCode) return null;

            return $"https://firebasestorage.googleapis.com/v0/b/{_storageBucket}/o/{objectPath}?alt=media";
        }

        public async Task<Character?> GetCharacterAsync(string characterId)
        {
            var url = $"{_baseUrl}/characters/{characterId}";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return ParseCharacter(json);
        }
    }
}
