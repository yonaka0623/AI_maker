using AiCharacterMaker.Services;

namespace AiCharacterMaker.Pages;

public partial class CharaCreatePage : ContentPage
{
    readonly FirebaseService _firebase = new();
    string? _iconLocalPath; // 選択した画像のローカルパス

    // voiceIdに変換する対応表
    readonly Dictionary<int, string> _voiceIdMap = new()
    {
        { 0, "Mizuki" },
        { 1, "Takumi" },
        { 2, "Kazuha" },
        { 3, "Tomoko" },
    };

    public CharaCreatePage()
    {
        InitializeComponent();
    }

    // ===== 戻るボタン =====
    async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//charaList");
    }

    // ===== アイコン画像選択 =====
    async void OnPickIconClicked(object sender, EventArgs e)
    {
        var result = await FilePicker.PickAsync(new PickOptions
        {
            FileTypes = FilePickerFileType.Images,
            PickerTitle = "アイコン画像を選択"
        });

        if (result == null) return;

        _iconLocalPath = result.FullPath;
        IconPreview.Source = ImageSource.FromFile(_iconLocalPath);
        IconPreview.IsVisible = true;
    }

    // ===== 作成ボタン =====
    async void OnCreateClicked(object sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim() ?? "";
        var shortPersonality = ShortPersonalityEntry.Text?.Trim() ?? "";
        var personality = PersonalityEditor.Text?.Trim() ?? "";
        var voiceIndex = VoicePicker.SelectedIndex;

        // バリデーション（TSの disabled 相当）
        if (string.IsNullOrEmpty(name) ||
            string.IsNullOrEmpty(shortPersonality) ||
            string.IsNullOrEmpty(personality) ||
            voiceIndex < 0)
        {
            await DisplayAlert("入力エラー", "すべての項目を入力してください", "OK");
            return;
        }

        var uid = await _firebase.GetCurrentUidAsync();
        if (uid == null)
        {
            await DisplayAlert("エラー", "ログインしてください", "OK");
            return;
        }

        var voiceId = _voiceIdMap[voiceIndex];

        try
        {
            CreateButton.IsEnabled = false;
            CreateButton.Text = "作成中…";

            // アイコンをStorageにアップロード（任意）
            string? iconUrl = null;
            if (_iconLocalPath != null)
                iconUrl = await _firebase.UploadIconAsync(uid, _iconLocalPath);

            // Firestoreにキャラを保存
            await _firebase.CreateCharacterAsync(new()
            {
                ["name"] = name,
                ["personality"] = personality,
                ["shortPersonality"] = shortPersonality,
                ["voiceId"] = voiceId,
                ["iconUrl"] = iconUrl,
                ["creator"] = uid,
            });

            await Shell.Current.GoToAsync("//charaList");
        }
        catch (Exception ex)
        {
            await DisplayAlert("エラー", $"キャラの作成に失敗しました: {ex.Message}", "OK");
        }
        finally
        {
            CreateButton.IsEnabled = true;
            CreateButton.Text = "作成";
        }
    }
}