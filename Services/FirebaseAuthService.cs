using System.Text;
using System.Text.Json;

namespace AiCharacterMaker.Services;

public class FirebaseAuthService
{
    const string ApiKey = "YOUR_FIREBASE_API_KEY"; // Firebaseコンソールのウェブ設定から取得
    readonly HttpClient _http = new();

    // ===== ログイン =====
    public async Task SignInAsync(string email, string password)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}";
        var body = JsonSerializer.Serialize(new
        {
            email,
            password,
            returnSecureToken = true
        });

        var res = await _http.PostAsync(url,
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (!res.IsSuccessStatusCode)
            throw new Exception("ログインに失敗しました");

        var json = await res.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        // idTokenとuidを保存
        var idToken = data.GetProperty("idToken").GetString()!;
        var uid = data.GetProperty("localId").GetString()!;

        await SecureStorage.SetAsync("firebase_id_token", idToken);
        await SecureStorage.SetAsync("firebase_uid", uid);
    }

    // ===== 新規登録 =====
    public async Task RegisterAsync(string email, string password)
    {
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}";
        var body = JsonSerializer.Serialize(new
        {
            email,
            password,
            returnSecureToken = true
        });

        var res = await _http.PostAsync(url,
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (!res.IsSuccessStatusCode)
            throw new Exception("新規登録に失敗しました");

        var json = await res.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        var idToken = data.GetProperty("idToken").GetString()!;
        var uid = data.GetProperty("localId").GetString()!;

        await SecureStorage.SetAsync("firebase_id_token", idToken);
        await SecureStorage.SetAsync("firebase_uid", uid);
    }

    // ===== ログアウト =====
    public Task SignOutAsync()
    {
        SecureStorage.Remove("firebase_id_token");
        SecureStorage.Remove("firebase_uid");
        return Task.CompletedTask;
    }
}