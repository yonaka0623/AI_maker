using CommunityToolkit.Maui.Media;

namespace AiCharacterMaker.Services;

public class SpeechService
{
    readonly ISpeechToText _stt = SpeechToText.Default;
    CancellationTokenSource? _cts;

    // ===== 音声認識開始 =====
    public async void StartListening(Action<string> onResult)
    {
        try
        {
            _cts = new CancellationTokenSource();

            var result = await _stt.ListenAsync(
                System.Globalization.CultureInfo.GetCultureInfo("ja-JP"),
                new Progress<string>(partialText =>
                {
                    // 途中結果（表示用）
                }),
                _cts.Token
            );

            if (result.IsSuccessful && !string.IsNullOrEmpty(result.Text))
            {
                onResult(result.Text);
            }
        }
        catch (OperationCanceledException)
        {
            // StopListening() で止めた場合は正常
        }
        catch (Exception ex)
        {
            Console.WriteLine($"STT error: {ex.Message}");
        }
    }

    // ===== 音声認識停止 =====
    public void StopListening()
    {
        _cts?.Cancel();
        _cts = null;
    }
}