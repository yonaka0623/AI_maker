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
            await ExecuteAuthAsync(() => _auth.SignInAsync(EmailEntry.Text?.Trim(), PasswordEntry.Text));
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            await ExecuteAuthAsync(() => _auth.RegisterAsync(EmailEntry.Text?.Trim(), PasswordEntry.Text));
        }

        private async Task ExecuteAuthAsync(Func<Task> authAction)
        {
            ErrorLabel.IsVisible = false;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            try
            {
                await authAction();
                _firebase.SetAuthToken(_auth.IdToken);
                await Shell.Current.GoToAsync("HomePage");
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = ex.Message;
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            }
        }
        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
