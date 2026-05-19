using AICharacterMaker.Services;

namespace AICharacterMaker.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly FirebaseAuthService _auth;
        private readonly FirebaseService _firebase;

        public LoginPage(FirebaseAuthService auth, FirebaseService firebase)
        {
            InitializeComponent();
            _auth = auth;
            _firebase = firebase;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await ExecuteAuthAsync(() => _auth.SignInAsync(EmailEntry.Text, PasswordEntry.Text));
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            await ExecuteAuthAsync(() => _auth.RegisterAsync(EmailEntry.Text, PasswordEntry.Text));
        }

        private async Task ExecuteAuthAsync(Func<Task<bool>> authAction)
        {
            ErrorLabel.IsVisible = false;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            try
            {
                var success = await authAction();
                if (success)
                {
                    _firebase.SetAuthToken(_auth.IdToken);
                    await Shell.Current.GoToAsync("//CharaListPage");
                }
                else
                {
                    ErrorLabel.Text = "認証に失敗しました。メールアドレスとパスワードを確認してください。";
                    ErrorLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"エラー: {ex.Message}";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }
    }
}
