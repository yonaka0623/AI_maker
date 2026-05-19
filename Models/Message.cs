namespace AICharacterMaker.Models
{
    public class Message
    {
        public string Id { get; set; } = string.Empty;
        public string CharacterId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Emotion { get; set; } = "neutral";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
