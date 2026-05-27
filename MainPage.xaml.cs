/*
using AiCharacterMaker.Models;
using AiCharacterMaker.Services;
using System.Text.Json;

namespace AiCharacterMaker;

public partial class MainPage : ContentPage
{
    // --- state ---
    string? _uid;
    string? _resolvedCharacterId;
    Character? _character;
    string _emotion = "neutral";
    bool _isProcessing = false;

    readonly FirebaseService _firebase = new();
    readonly SpeechService _speech = new();

    public MainPage()
    {
        InitializeComponent();
    }

    // ===== 画面表示時 =====
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1) ログイン確認
        _uid = await _firebase.GetCurrentUidAsync();
        UpdateHeaderButton();

        if (_uid == null) return;

        // 2) selectedCharacterIdを取得
        _resolvedCharacterId = await _firebase.GetSelectedCharacterIdAsync(_uid);
        if (_resolvedCharacterId == null) return;

        // 3) キャラ情報を取得
        await LoadCharacterAsync();
    }

    async Task LoadCharacterAsync()
    {
        if (_resolvedCharacterId == null) return;

        _character = await _firebase.GetCharacterAsync(_resolvedCharacterId);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_character?.ModelUrl != null)
            {
                // WebViewにVRMビューアを表示
                VrmWebView.IsVisible = true;
                CharaStatusLabel.IsVisible = false;
                VrmWebView.Source = new UrlWebViewSource
                {
                    Url = $"http://localhost/vrm-viewer.html?modelUrl={Uri.EscapeDataString(_character.ModelUrl)}"
                };
            }
            else
            {
                VrmWebView.IsVisible = false;
                CharaStatusLabel.IsVisible = true;
                CharaStatusLabel.Text = _character != null ? "VRMモデル未設定" : "読み込み中…";
            }

            VoiceArea.IsVisible = _character != null;
            UpdateVoiceButton("idle");
        });
    }

    // ===== ヘッダーボタン =====
    void UpdateHeaderButton()
    {
        HeaderButton.Text = _uid != null ? "設定" : "ログイン / 新規登録";
        HeaderButton.BackgroundColor = _uid != null
            ? Color.FromArgb("#E5E7EB")
            : Color.FromArgb("#3B82F6");
    }

    void OnHeaderButtonClicked(object sender, EventArgs e)
    {
        if (_uid != null)
            Shell.Current.GoToAsync("//settings");
        else
            Shell.Current.GoToAsync("//login");
    }

    async void OnLogoutTapped(object sender, TappedEventArgs e)
    {
        await _firebase.SignOutAsync();
        _uid = null;
        UpdateHeaderButton();
    }

    // ===== 音声ボタン =====
    void OnVoiceButtonPressed(object sender, EventArgs e)
    {
        _speech.StartListening(onResult: async (text) =>
        {
            await SendVoiceMessageAsync(text);
        });
        UpdateVoiceButton("listening");
    }

    void OnVoiceButtonReleased(object sender, EventArgs e)
    {
        _speech.StopListening();
    }

    void UpdateVoiceButton(string state)
    {
        VoiceStateLabel.Text = state switch
        {
            "idle"      => "状態：待機",
            "listening" => "状態：聞いてる…",
            "thinking"  => "状態：考え中…",
            "speaking"  => "状態：話し中…",
            _           => ""
        };
        VoiceButton.Text = state == "listening" ? "話し中（離すと送信）" : "押して話す";
        VoiceButton.BackgroundColor = state == "listening"
            ? Color.FromArgb("#EF4444")
            : Color.FromArgb("#3B82F6");
        VoiceButton.IsEnabled = state is "idle" or "listening";
    }

    // ===== メッセージ送信 =====
    async Task SendVoiceMessageAsync(string text)
    {
        if (_uid == null || _resolvedCharacterId == null || _character == null) return;
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;
        if (_isProcessing) return;
        _isProcessing = true;

        try
        {
            MainThread.BeginInvokeOnMainThread(() => UpdateVoiceButton("thinking"));

            var threadId = $"{_uid}_{_resolvedCharacterId}";

            // Firestoreに保存
            await _firebase.SaveThreadAsync(threadId, _uid, _resolvedCharacterId);
            await _firebase.AddMessageAsync(threadId, "user", trimmed);

            // ChatGPT API呼び出し
            var result = await ChatService.SendAsync(
                _resolvedCharacterId,
                _character.Personality,
                trimmed,
                threadId
            );

            if (result == null) return;

            // emotion → WebViewに送る
            _emotion = result.Emotion ?? "neutral";
            await VrmWebView.EvaluateJavaScriptAsync($"setEmotion('{_emotion}')");

            // AI返答を表示
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AiTextLabel.Text = result.Text;
                AiTextBox.IsVisible = true;
            });

            // Firestoreに保存
            await _firebase.AddMessageAsync(threadId, "assistant", result.Text);

            // TTS再生
            MainThread.BeginInvokeOnMainThread(() => UpdateVoiceButton("speaking"));
            var polly = new PollyService();
            await polly.SpeakAsync(result.Text, _character.VoiceId);
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AiTextLabel.Text = $"エラー: {ex.Message}";
                AiTextBox.IsVisible = true;
            });
        }
        finally
        {
            _isProcessing = false;
            MainThread.BeginInvokeOnMainThread(() => UpdateVoiceButton("idle"));
        }
    }
}
*/