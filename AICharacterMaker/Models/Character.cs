namespace AICharacterMaker.Models
{
    public class Character
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Personality { get; set; } = string.Empty;
        public string VrmUrl { get; set; } = string.Empty;
        public string TtsVoice { get; set; } = "Mizuki";
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
