using AiCharacterMaker.Services;

namespace AiCharacterMaker.Pages;

public partial class LoginPage : ContentPage
{
    readonly FirebaseAuthService _auth = new();

    public LoginPage()
    {
        InitializeComponent();
    }

    // ===== モード切り替え =====
    void OnSwitchToRegisterTapped(object sender, TappedEventArgs e)
    {
        LoginArea.IsVisible = false;
        RegisterArea.IsVisible = true;
    }

    void OnSwitchToLoginTapped(object sender, TappedEventArgs e)
    {
        RegisterArea.IsVisible = false;
        LoginArea.IsVisible = true;
    }

    // ===== ホームへ戻る =====
    async void OnHomeClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//main");
    }

    // ===== ログイン =====
    async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            await _auth.SignInAsync(LoginEmailEntry.Text, LoginPasswordEntry.Text);
            await Shell.Current.GoToAsync("//main");
        }
        catch
        {
            await DisplayAlert("エラー", "ログインに失敗しました", "OK");
        }
    }

    // ===== 新規登録 =====
    async void OnRegisterClicked(object sender, EventArgs e)
    {
        if (RegisterPasswordEntry.Text != PasswordConfirmEntry.Text)
        {
            await DisplayAlert("エラー", "パスワードが一致しません", "OK");
            return;
        }

        try
        {
            await _auth.RegisterAsync(RegisterEmailEntry.Text, RegisterPasswordEntry.Text);
            await Shell.Current.GoToAsync("//main");
        }
        catch
        {
            await DisplayAlert("エラー", "新規登録に失敗しました", "OK");
        }
    }
}