using AICharacterMaker.Models;
using AICharacterMaker.Services;

namespace AICharacterMaker.Pages
{
    public partial class HomePage : ContentPage
    {
        private readonly FirebaseAuthService _auth;
        private readonly FirebaseService _firebase;

        public HomePage(FirebaseAuthService auth, FirebaseService firebase)
        {
            InitializeComponent();
            _auth = auth;
            _firebase = firebase;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSelectedCharacterAsync();
        }

        private async Task LoadSelectedCharacterAsync()
        {
            if (_auth.UserId == null) return;

            var selectedId = await _firebase.GetSelectedCharacterIdAsync(_auth.UserId);

            if (string.IsNullOrEmpty(selectedId))
            {
                // キャラ未選択
                NoCharaMessage.IsVisible = true;
                VrmNotSetMessage.IsVisible = false;
                TalkControls.IsVisible = false;
                return;
            }

            var chara = await _firebase.GetCharacterAsync(selectedId);
            if (chara == null)
            {
                NoCharaMessage.IsVisible = true;
                VrmNotSetMessage.IsVisible = false;
                TalkControls.IsVisible = false;
                return;
            }

            // キャラ選択済み
            NoCharaMessage.IsVisible = false;
            TalkControls.IsVisible = true;

            // VRMの有無で表示切り替え
            VrmNotSetMessage.IsVisible = string.IsNullOrEmpty(chara.VrmUrl);
        }

        private void OnTalkClicked(object sender, EventArgs e)
        {
            // TODO: 音声認識（STT）接続
            StatusLabel.Text = "状態：話し中...";
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            // TODO: 設定画面
        }

        private async void OnChatTabClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("ChatPage");
        }

        private async void OnCharaListTabClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("CharaListPage");
        }
    }
}