using Microsoft.Extensions.Configuration;

namespace AICharacterMaker;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        // appsettings.jsonからキーを読み込む
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // AWSキーを安全に保存（初回起動時のみ）
        if (await SecureStorage.GetAsync("aws_access_key") == null)
        {
            await SecureStorage.SetAsync("aws_access_key", config["AWS:AccessKeyId"] ?? "");
            await SecureStorage.SetAsync("aws_secret_key", config["AWS:SecretAccessKey"] ?? "");
        }
    }
}