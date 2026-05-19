using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;

namespace AICharacterMaker.Services
{
    public class PollyService
    {
        private readonly AmazonPollyClient _client;

        private static readonly HashSet<string> NeuralVoices =
            new(StringComparer.OrdinalIgnoreCase) { "Kazuha", "Tomoko" };

        public PollyService(string accessKey, string secretKey, string region = "ap-northeast-1")
        {
            _client = new AmazonPollyClient(
                accessKey, secretKey,
                RegionEndpoint.GetBySystemName(region));
        }

        public async Task<byte[]?> SynthesizeSpeechAsync(string text, string voiceId = "Mizuki")
        {
            var request = new SynthesizeSpeechRequest
            {
                Text = text,
                VoiceId = voiceId,
                OutputFormat = OutputFormat.Mp3,
                Engine = NeuralVoices.Contains(voiceId) ? Engine.Neural : Engine.Standard
            };

            try
            {
                var response = await _client.SynthesizeSpeechAsync(request);
                using var ms = new MemoryStream();
                await response.AudioStream.CopyToAsync(ms);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
