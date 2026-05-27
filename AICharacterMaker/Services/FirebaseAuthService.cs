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

        public async Task SignInAsync(string email, string password)
        {
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("Firebase APIキーが設定されていません。appsettings.json を確認してください。");

            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";
            var payload = new { email, password, returnSecureToken = true };
            var response = await _http.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
                await ThrowFirebaseErrorAsync(response);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            IdToken = json.GetProperty("idToken").GetString();
            UserId = json.GetProperty("localId").GetString();
        }

        public async Task RegisterAsync(string email, string password)
        {
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("Firebase APIキーが設定されていません。appsettings.json を確認してください。");

            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
            var payload = new { email, password, returnSecureToken = true };
            var response = await _http.PostAsJsonAsync(url, payload);

            if (!response.IsSuccessStatusCode)
                await ThrowFirebaseErrorAsync(response);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            IdToken = json.GetProperty("idToken").GetString();
            UserId = json.GetProperty("localId").GetString();
        }

        public void SignOut()
        {
            IdToken = null;
            UserId = null;
        }

        private static async Task ThrowFirebaseErrorAsync(HttpResponseMessage response)
        {
            string? errorCode = null;
            try
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                errorCode = body.GetProperty("error").GetProperty("message").GetString();
            }
            catch { }

            throw new Exception(errorCode != null
                ? ToJapanese(errorCode)
                : $"認証サーバーエラー ({(int)response.StatusCode})");
        }
        private static string ToJapanese(string code)
        {
            if (code.StartsWith("WEAK_PASSWORD")) return "パスワードは6文字以上にしてください。";
            return code switch
            {
                "EMAIL_NOT_FOUND" => "このメールアドレスは登録されていません。",
                "INVALID_PASSWORD" => "パスワードが正しくありません。",
                "INVALID_LOGIN_CREDENTIALS" => "メールアドレスまたはパスワードが正しくありません。",
                "USER_DISABLED" => "このアカウントは無効化されています。",
                "EMAIL_EXISTS" => "このメールアドレスはすでに登録されています。",
                "INVALID_EMAIL" => "メールアドレスの形式が正しくありません。",
                "MISSING_PASSWORD" => "パスワードを入力してください。",
                "MISSING_EMAIL" => "メールアドレスを入力してください。",
                "API_KEY_INVALID" => "Firebase APIキーが無効です。appsettings.json を確認してください。",
                "TOO_MANY_ATTEMPTS_TRY_LATER" => "試行回数が多すぎます。しばらく待ってから再試行してください。",
                _ => $"認証エラー: {code}"
            };
        }
    }
}