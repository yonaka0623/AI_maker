using AICharacterMaker.Models;
using AICharacterMaker.Services;

namespace AICharacterMaker.Pages
{
    public partial class CharaCreatePage : ContentPage
    {
        private readonly FirebaseAuthService _auth;
        private readonly FirebaseService _firebase;
        private string? _iconUrl;

        public CharaCreatePage(FirebaseAuthService auth, FirebaseService firebase)
        {
            InitializeComponent();
            _auth = auth;
            _firebase = firebase;
        }

        private async void OnSelectIconClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "アイコン画像を選択",
                    FileTypes = FilePickerFileType.Images
                });

                if (result == null) return;

                UploadIndicator.IsVisible = true;
                UploadIndicator.IsRunning = true;
                IconFileLabel.Text = $"アップロード中...";

                using var stream = await result.OpenReadAsync();
                _iconUrl = await _firebase.UploadIconAsync(
                    _auth.UserId ?? "unknown",
                    result.FileName,
                    stream);

                IconFileLabel.Text = _iconUrl != null
                    ? result.FileName
                    : "アップロードに失敗しました";
            }
            catch (Exception ex)
            {
                IconFileLabel.Text = $"エラー: {ex.Message}";
            }
            finally
            {
                UploadIndicator.IsRunning = false;
                UploadIndicator.IsVisible = false;
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            if (string.IsNullOrWhiteSpace(NameEntry.Text))
            {
                ErrorLabel.Text = "キャラの名前を入力してください";
                ErrorLabel.IsVisible = true;
                return;
            }

            var character = new Character
            {
                Name = NameEntry.Text.Trim(),
                ShortDescription = ShortDescEntry.Text?.Trim() ?? string.Empty,
                Personality = PersonalityEditor.Text?.Trim() ?? string.Empty,
                IconUrl = _iconUrl ?? string.Empty,
                TtsVoice = VoicePicker.SelectedItem?.ToString() ?? "Mizuki",
                Creator = _auth.UserId ?? string.Empty
            };

            var id = await _firebase.CreateCharacterAsync(character);
            if (id == null)
            {
                ErrorLabel.Text = "保存に失敗しました";
                ErrorLabel.IsVisible = true;
                return;
            }

            await Shell.Current.GoToAsync("..");
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}