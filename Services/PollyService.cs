using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;

namespace AiCharacterMaker.Services;

public class PollyService
{
    readonly AmazonPollyClient _client;

    // neuralエンジン対応の声
    static readonly HashSet<string> NeuralVoices = new() { "Kazuha", "Tomoko" };

    public PollyService()
    {
        _client = new AmazonPollyClient(
            awsAccessKeyId:     "YOUR_AWS_ACCESS_KEY_ID",
            awsSecretAccessKey: "YOUR_AWS_SECRET_ACCESS_KEY",
            region: RegionEndpoint.APNortheast1
        );
    }

    public async Task SpeakAsync(string text, string voiceId)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(voiceId)) return;

        var engine = NeuralVoices.Contains(voiceId) ? Engine.Neural : Engine.Standard;

        var request = new SynthesizeSpeechRequest
        {
            OutputFormat = OutputFormat.Mp3,
            Text         = text,
            VoiceId      = voiceId,
            Engine       = engine,
        };

        var response = await _client.SynthesizeSpeechAsync(request);

        // 音声ストリームを一時ファイルに書き出して再生
        var tmpPath = Path.Combine(FileSystem.CacheDirectory, "tts_output.mp3");
        await using (var fs = File.Create(tmpPath))
        {
            await response.AudioStream.CopyToAsync(fs);
        }

        // MAUIのMediaPlayerで再生
        var player = AudioManager.Current.CreatePlayer(tmpPath);
        player.Play();
    }
}