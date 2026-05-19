using AICharacterMaker.Models;
using AICharacterMaker.Services;
using Plugin.Maui.Audio;
using System.Globalization;

namespace AICharacterMaker.Pages
{
    // Value converters for message bubbles
    public class RoleToAlignmentConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string role && role == "user" ? LayoutOptions.End : LayoutOptions.Start;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class RoleToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string role && role == "user" ? Color.FromArgb("#7C3AED") : Color.FromArgb("#374151");

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    [QueryProperty(nameof(Character), "Character")]
    public partial class ChatPage : ContentPage
    {
        private readonly FirebaseService _firebase;
        private readonly ChatService _chat;
        private readonly PollyService _polly;
        private readonly SpeechService _speech;
        private readonly IAudioManager _audioManager;

        private Character? _character;
        private List<Message> _messages = [];
        private CancellationTokenSource? _micCts;
        private bool _isProcessing;

        public Character? Character
        {
            get => _character;
            set
            {
                _character = value;
                if (_character != null)
                    OnCharacterSet();
            }
        }

        public ChatPage(
            FirebaseService firebase,
            ChatService chat,
            PollyService polly,
            SpeechService speech,
            IAudioManager audioManager)
        {
            InitializeComponent();
            _firebase = firebase;
            _chat = chat;
            _polly = polly;
            _speech = speech;
            _audioManager = audioManager;
        }

        private void OnCharacterSet()
        {
            if (_character == null) return;
            CharaNameLabel.Text = _character.Name;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_character == null) return;

            VrmWebView.Source = new HtmlWebViewSource
            {
                Html = await LoadVrmHtmlAsync()
            };

            _messages = await _firebase.GetMessagesAsync(_character.Id);
            RefreshMessageList();
        }

        private static async Task<string> LoadVrmHtmlAsync()
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("vrm-viewer.html");
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.StartsWith("app://callback")) return;
            e.Cancel = true;

            var message = Uri.UnescapeDataString(e.Url.Replace("app://callback?", ""));
            if (message == "vrm_loaded" && _character != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    VrmWebView.EvaluateJavaScriptAsync($"loadVRM('{_character.VrmUrl}')"));
            }
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            var text = TextInput.Text?.Trim();
            if (string.IsNullOrEmpty(text) || _isProcessing) return;
            TextInput.Text = string.Empty;
            await ProcessUserInputAsync(text);
        }

        private async void OnMicClicked(object sender, EventArgs e)
        {
            if (_isProcessing) return;

            MicButton.Text = "⏹";
            _micCts = new CancellationTokenSource();

            try
            {
                var text = await _speech.ListenAsync(_micCts.Token);
                if (!string.IsNullOrEmpty(text))
                    await ProcessUserInputAsync(text);
            }
            finally
            {
                MicButton.Text = "🎤";
                _micCts?.Dispose();
                _micCts = null;
            }
        }

        private async Task ProcessUserInputAsync(string userText)
        {
            if (_character == null || _isProcessing) return;
            _isProcessing = true;

            var userMsg = new Message
            {
                CharacterId = _character.Id,
                Role = "user",
                Text = userText,
                Timestamp = DateTime.UtcNow
            };
            _messages.Add(userMsg);
            RefreshMessageList();
            await _firebase.SaveMessageAsync(userMsg);

            try
            {
                var result = await _chat.SendMessageAsync(_character, _messages, userText);
                if (result == null) return;

                var (emotion, replyText) = result.Value;

                await VrmWebView.EvaluateJavaScriptAsync($"setExpression('{emotion}')");

                var assistantMsg = new Message
                {
                    CharacterId = _character.Id,
                    Role = "assistant",
                    Text = replyText,
                    Emotion = emotion,
                    Timestamp = DateTime.UtcNow
                };
                _messages.Add(assistantMsg);
                RefreshMessageList();
                await _firebase.SaveMessageAsync(assistantMsg);

                await PlaySpeechAsync(replyText);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private async Task PlaySpeechAsync(string text)
        {
            if (_character == null) return;
            var audioBytes = await _polly.SynthesizeSpeechAsync(text, _character.TtsVoice);
            if (audioBytes == null) return;

            var stream = new MemoryStream(audioBytes);
            var player = _audioManager.CreatePlayer(stream);
            player.Play();
        }

        private void RefreshMessageList()
        {
            MessageCollection.ItemsSource = null;
            MessageCollection.ItemsSource = _messages;
            MessageCollection.ScrollTo(_messages.Count - 1, animate: false);
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
