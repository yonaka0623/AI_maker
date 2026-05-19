using AiCharacterMaker.Models;
using AiCharacterMaker.Services;
using System.Collections.ObjectModel;

namespace AiCharacterMaker.Pages;

public partial class CharaListPage : ContentPage
{
    string? _resolvedCharacterId;
    string _sortOrder = "desc";

    readonly FirebaseService _firebase = new();
    readonly ObservableCollection<Character> _characters = new();
    IDispatcherTimer? _pollTimer;

    public CharaListPage()
    {
        InitializeComponent();
        CharaListView.ItemsSource = _characters;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var uid = await _firebase.GetCurrentUidAsync();
        if (uid == null) return;

        // selectedCharacterIdを取得
        _resolvedCharacterId = await _firebase.GetSelectedCharacterIdAsync(uid);
        Footer.CharacterId = _resolvedCharacterId;

        // キャラ一覧取得 & ポーリング開始
        await LoadCharactersAsync();
        StartPolling();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _pollTimer?.Stop();
    }

    // ===== キャラ一覧取得 =====
    async Task LoadCharactersAsync()
    {
        var uid = await _firebase.GetCurrentUidAsync();
        if (uid == null) return;

        var list = await _firebase.GetCharactersAsync(uid, _sortOrder);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _characters.Clear();
            foreach (var c in list) _characters.Add(c);
            UpdateSelectedHighlight();
        });
    }

    // 選択中キャラのカードをハイライト
    void UpdateSelectedHighlight()
    {
        // CollectionView内のカードの色を選択状態で変える
        // ※ シンプルにするため FirebaseService で取得後に判定
        foreach (var c in _characters)
        {
            // Converterを使う方法の代わりに、IsSelectedプロパティをCharacterに追加するのが理想
        }
    }

    void StartPolling()
    {
        _pollTimer = Dispatcher.CreateTimer();
        _pollTimer.Interval = TimeSpan.FromSeconds(5);
        _pollTimer.Tick += async (s, e) => await LoadCharactersAsync();
        _pollTimer.Start();
    }

    // ===== キャラ選択 =====
    async void OnCharaTapped(object sender, TappedEventArgs e)
    {
        var characterId = e.Parameter as string;
        if (characterId == null) return;

        var uid = await _firebase.GetCurrentUidAsync();
        if (uid == null) return;

        // Firestoreに選択キャラを保存
        await _firebase.SetSelectedCharacterAsync(uid, characterId);
        _resolvedCharacterId = characterId;
        Footer.CharacterId = characterId;

        await Shell.Current.GoToAsync($"//charaList?characterId={characterId}");
    }

    // ===== 編集ボタン =====
    async void OnEditClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        var characterId = btn?.CommandParameter as string;
        if (characterId == null) return;

        await Shell.Current.GoToAsync($"//charaFix?characterId={characterId}");
    }

    // ===== 並び替え =====
    async void OnSortDescClicked(object sender, EventArgs e)
    {
        _sortOrder = "desc";
        SortDescButton.BackgroundColor = Color.FromArgb("#3B82F6");
        SortDescButton.TextColor = Colors.White;
        SortAscButton.BackgroundColor = Color.FromArgb("#E5E7EB");
        SortAscButton.TextColor = Colors.Black;
        await LoadCharactersAsync();
    }

    async void OnSortAscClicked(object sender, EventArgs e)
    {
        _sortOrder = "asc";
        SortAscButton.BackgroundColor = Color.FromArgb("#3B82F6");
        SortAscButton.TextColor = Colors.White;
        SortDescButton.BackgroundColor = Color.FromArgb("#E5E7EB");
        SortDescButton.TextColor = Colors.Black;
        await LoadCharactersAsync();
    }

    // ===== 新規作成 =====
    async void OnCreateClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//charaCreate");
    }
}