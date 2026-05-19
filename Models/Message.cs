namespace AiCharacterMaker.Models;

public class Message
{
    public string Id { get; set; } = "";
    public string Role { get; set; } = "user"; // "user" or "assistant"
    public string Text { get; set; } = "";
}