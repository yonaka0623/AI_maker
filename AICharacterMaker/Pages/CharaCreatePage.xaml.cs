using AICharacterMaker.Models;
using AICharacterMaker.Services;

namespace AICharacterMaker.Pages
{
    public partial class CharaCreatePage : ContentPage
    {
        private readonly FirebaseAuthService _auth;
        private readonly FirebaseService _firebase;
        private string? _vrmUrl;

        public CharaCreatePage(FirebaseAuthService auth, FirebaseService firebase)
        {
            InitializeComponent();
            _auth = auth;
            _firebase = firebase;
            VoicePicker.SelectedIndex = 0;
        }

        private async void OnSelectVrmClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "VRMファイルを選択",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        [DevicePlatform.Android] = ["application/octet-stream"],
                        [DevicePlatform.iOS] = ["public.data"],
                        [DevicePlatform.WinUI] = [".vrm"]
                    })
                });

                if (result == null) return;

                UploadIndicator.IsVisible = true;
                UploadIndicator.IsRunning = true;
                VrmFileLabel.Text = $"アップロード中: {result.FileName}";

                using var stream = await result.OpenReadAsync();
                _vrmUrl = await _firebase.UploadVrmAsync(
                    _auth.UserId ?? "unknown",
                    result.FileName,
                    stream);

                VrmFileLabel.Text = _vrmUrl != null
                    ? $"✓ {result.FileName}"
                    : "アップロードに失敗しました";
            }
            catch (Exception ex)
            {
                VrmFileLabel.Text = $"エラー: {ex.Message}";
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
                ErrorLabel.Text = "キャラクター名を入力してください";
                ErrorLabel.IsVisible = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(PersonalityEditor.Text))
            {
                ErrorLabel.Text = "性格・設定を入力してください";
                ErrorLabel.IsVisible = true;
                return;
            }

            var character = new Character
            {
                Name = NameEntry.Text.Trim(),
                Personality = PersonalityEditor.Text.Trim(),
                VrmUrl = _vrmUrl ?? string.Empty,
                TtsVoice = VoicePicker.SelectedItem?.ToString() ?? "Mizuki",
                UserId = _auth.UserId ?? string.Empty
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
