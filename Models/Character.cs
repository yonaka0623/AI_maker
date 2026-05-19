namespace AiCharacterMaker.Models;

public class Character
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Personality { get; set; } = "";
    public string VoiceId { get; set; } = "";
    public string? ShortPersonality { get; set; }
    public string? IconUrl { get; set; }
    public string? ModelUrl { get; set; }
}