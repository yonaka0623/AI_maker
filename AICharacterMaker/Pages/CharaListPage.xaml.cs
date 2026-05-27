using AICharacterMaker.Models;
using AICharacterMaker.Services;

namespace AICharacterMaker.Pages
{
    public partial class CharaListPage : ContentPage
    {
        private readonly FirebaseAuthService _auth;
        private readonly FirebaseService _firebase;
        private List<Character> _characters = [];
        private bool _sortNewest = true;

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
            var selectedId = await _firebase.GetSelectedCharacterIdAsync(_auth.UserId);

            foreach (var c in _characters)
                c.IsSelected = c.Id == selectedId;

            ApplySort();
        }

        private void ApplySort()
        {
            var sorted = _sortNewest
                ? _characters.OrderByDescending(c => c.CreatedAt).ToList()
                : _characters.OrderBy(c => c.CreatedAt).ToList();
            CharaCollection.ItemsSource = sorted;
        }

        private async void OnCharaTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not Character chara || _auth.UserId == null) return;

            await _firebase.SetSelectedCharacterAsync(_auth.UserId, chara.Id);

            foreach (var c in _characters)
                c.IsSelected = c.Id == chara.Id;

            ApplySort();
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Character chara)
            {
                var param = new Dictionary<string, object> { ["Character"] = chara };
                await Shell.Current.GoToAsync("CharaCreatePage", param);
            }
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("CharaCreatePage");
        }

        private void OnNewOrderClicked(object sender, EventArgs e)
        {
            _sortNewest = true;
            NewOrderBtn.BackgroundColor = Color.FromArgb("#007AFF");
            NewOrderBtn.TextColor = Colors.White;
            OldOrderBtn.BackgroundColor = Color.FromArgb("#E0E0E0");
            OldOrderBtn.TextColor = Color.FromArgb("#333333");
            ApplySort();
        }

        private void OnOldOrderClicked(object sender, EventArgs e)
        {
            _sortNewest = false;
            OldOrderBtn.BackgroundColor = Color.FromArgb("#007AFF");
            OldOrderBtn.TextColor = Colors.White;
            NewOrderBtn.BackgroundColor = Color.FromArgb("#E0E0E0");
            NewOrderBtn.TextColor = Color.FromArgb("#333333");
            ApplySort();
        }

        private async void OnHomeTabClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("HomePage");
        }

        private async void OnChatTabClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("ChatPage");
        }
    }
}