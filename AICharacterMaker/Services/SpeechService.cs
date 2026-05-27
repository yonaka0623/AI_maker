using System.Globalization;

namespace AICharacterMaker.Services
{
    public class SpeechService
    {
        public async Task<string?> ListenAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            return null;
        }
    }
}
/*
using CommunityToolkit.Maui.Media;
using System.Globalization;

namespace AICharacterMaker.Services
{
    public class SpeechService
    {
        private readonly ISpeechToText _stt;

        public SpeechService(ISpeechToText stt)
        {
            _stt = stt;
        }

        public async Task<string?> ListenAsync(CancellationToken cancellationToken = default)
        {
            var permission = await Permissions.RequestAsync<Permissions.Microphone>();
            if (permission != PermissionStatus.Granted) return null;

            try
            {
            var result = await _stt.ListenAsync(
                CultureInfo.GetCultureInfo("ja-JP"),
                cancellationToken);
            return result.IsSuccessful ? result.Text : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
*/