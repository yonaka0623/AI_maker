using CommunityToolkit.Maui.Media;
using AICharacterMaker.Services;
using CommunityToolkit.Maui;
//using CommunityToolkit.Maui.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
//using Plugin.Maui.Audio;

namespace AICharacterMaker
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                //.UseAudio()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Load appsettings.json (embedded resource, .gitignored locally)
            var assembly = typeof(MauiProgram).Assembly;
            var stream = assembly.GetManifestResourceStream("AICharacterMaker.appsettings.json");
            if (stream != null)
                builder.Configuration.AddJsonStream(stream);

            var cfg = builder.Configuration;

            builder.Services.AddSingleton(new FirebaseAuthService(
                cfg["Firebase:ApiKey"] ?? string.Empty));

            builder.Services.AddSingleton(new FirebaseService(
                cfg["Firebase:ProjectId"] ?? string.Empty,
                cfg["Firebase:StorageBucket"] ?? string.Empty));

            builder.Services.AddSingleton(new ChatService(
                cfg["OpenAI:ApiKey"] ?? string.Empty,
                cfg["OpenAI:Model"] ?? "gpt-4o-mini"));

            builder.Services.AddSingleton(new PollyService(
                cfg["AWS:AccessKey"] ?? string.Empty,
                cfg["AWS:SecretKey"] ?? string.Empty,
                cfg["AWS:Region"] ?? "ap-northeast-1"));

            builder.Services.AddSingleton<SpeechService>();
            builder.Services.AddTransient<Pages.HomePage>();
            builder.Services.AddTransient<Pages.LoginPage>();
            builder.Services.AddTransient<Pages.CharaListPage>();
            builder.Services.AddTransient<Pages.ChatPage>();
            builder.Services.AddTransient<Pages.CharaCreatePage>();
            //builder.Services.AddSingleton(SpeechToText.Default);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
