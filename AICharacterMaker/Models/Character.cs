namespace AICharacterMaker.Models
{
    public class Character
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Personality { get; set; } = string.Empty;
        public string VrmUrl { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string TtsVoice { get; set; } = "Mizuki";
        public string Creator { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsSelected { get; set; }
    }
}