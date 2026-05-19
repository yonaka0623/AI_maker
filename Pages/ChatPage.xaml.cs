using AiCharacterMaker.Models;
using AiCharacterMaker.Services;
using System.Collections.ObjectModel;

namespace AiCharacterMaker.Pages;

public partial class ChatPage : ContentPage
{
    string? _characterId;
    string? _threadId;
    dynamic? _character;

    readonly FirebaseService _firebase = new();
    readonly ObservableCollection<Message> _messages = new();

    // メッセージ一覧を定期更新するタイマー
    // （TSの onSnapshot 相当）
    IDispatcherTimer? _pollTimer;

    public ChatPage()
    {
        InitializeComponent();
        MessagesView.ItemsSource = _messages;
    }

    // ===== クエリパラメータ受け取り =====
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Shell のクエリパラメータから characterId を取得
        if (Shell.Current.CurrentState.Location.ToString().Contains("characterId="))
        {
            var uri = Shell.Current.CurrentState.Location.ToString();
            _characterId = System.Web.HttpUtility.ParseQueryString(
                new Uri("http://x" + uri.Substring(uri.IndexOf('?'))).Query
            )["characterId"];
        }

        if (_characterId == null) return;

        // キャラ情報取得
        _character = await _firebase.GetCharacterAsync(_characterId);
        CharaNameLabel.Text = _character?.Name ?? "不明";

        // threadId
        var uid = await _firebase.GetCurrentUidAsync();
        if (uid == null) return;
        _threadId = $"{uid}_{_characterId}";

        // メッセージ取得 & 定期ポーリング開始
        await LoadMessagesAsync();
        StartPolling();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _pollTimer?.Stop();
    }

    // ===== メッセージ取得 =====
    // onSnapshot の代わりに3秒ごとにFetch
    async Task LoadMessagesAsync()
    {
        if (_threadId == null) return;

        var fetched = await _firebase.GetMessagesAsync(_threadId);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _messages.Clear();
            foreach (var m in fetched) _messages.Add(m);

            // 最下部にスクロール
            if (_messages.Count > 0)
                MessagesView.ScrollTo(_messages[^1], ScrollToPosition.End, false);
        });
    }

    void StartPolling()
    {
        _pollTimer = Dispatcher.CreateTimer();
        _pollTimer.Interval = TimeSpan.FromSeconds(3);
        _pollTimer.Tick += async (s, e) => await LoadMessagesAsync();
        _pollTimer.Start();
    }

    // ===== 戻るボタン =====
    async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//charaList");
    }

    // ===== 送信 =====
    async void OnSendClicked(object sender, EventArgs e)
    {
        var text = InputEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text) || _threadId == null || _character == null) return;

        InputEntry.Text = "";

        // Firestoreに保存
        await _firebase.SaveThreadAsync(_threadId, "", _characterId!);
        await _firebase.AddMessageAsync(_threadId, "user", text);

        // GPT API呼び出し
        var result = await ChatService.SendAsync(
            _characterId!,
            _character.Personality,
            text,
            _threadId
        );

        if (result == null) return;

        // アシスタントの返答を保存
        await _firebase.AddMessageAsync(_threadId, "assistant", result.Text);

        // メッセージ更新
        await LoadMessagesAsync();
    }
}