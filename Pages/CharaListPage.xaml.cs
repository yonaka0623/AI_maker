using AICharacterMaker.Models;
using AICharacterMaker.Services;

namespace AICharacterMaker.Pages
{
    public partial class CharaListPage : ContentPage
    {
        private readonly FirebaseAuthService _auth;
        private readonly FirebaseService _firebase;
        private List<Character> _characters = [];

        public CharaListPage(FirebaseAuthService auth, FirebaseService firebase)
        {
            InitializeComponent();
            _auth = auth;
            _firebase = firebase;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCharactersAsync();
        }

        private async Task LoadCharactersAsync()
        {
            if (_auth.UserId == null) return;
            _characters = await _firebase.GetCharactersAsync(_auth.UserId);
            CharaCollection.ItemsSource = _characters;
        }

        private async void OnStartChatClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Character chara)
            {
                var param = new Dictionary<string, object> { ["Character"] = chara };
                await Shell.Current.GoToAsync("ChatPage", param);
            }
        }

        private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is Character chara)
            {
                var confirmed = await DisplayAlert("確認", $"「{chara.Name}」を削除しますか？", "削除", "キャンセル");
                if (!confirmed) return;
                await _firebase.DeleteCharacterAsync(chara.Id);
                await LoadCharactersAsync();
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            _auth.SignOut();
            _firebase.SetAuthToken(null);
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
