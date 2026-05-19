using System.Net.Http.Json;
using System.Text.Json;

namespace AICharacterMaker.Services
{
    public class FirebaseAuthService
    {
        private readonly HttpClient _http = new();
        private readonly string _apiKey;

        public string? IdToken { get; private set; }
        public string? UserId { get; private set; }
        public bool IsSignedIn => IdToken != null;

        public FirebaseAuthService(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<bool> SignInAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";
            var payload = new { email, password, returnSecureToken = true };
            var response = await _http.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            IdToken = json.GetProperty("idToken").GetString();
            UserId = json.GetProperty("localId").GetString();
            return true;
        }

        public async Task<bool> RegisterAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
            var payload = new { email, password, returnSecureToken = true };
            var response = await _http.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            IdToken = json.GetProperty("idToken").GetString();
            UserId = json.GetProperty("localId").GetString();
            return true;
        }

        public void SignOut()
        {
            IdToken = null;
            UserId = null;
        }
    }
}
