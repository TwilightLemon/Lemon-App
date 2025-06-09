namespace LemonApp.MusicLib.Abstraction.Entities;

public class LyricData
{
    public Platform Platform { get; set; } = Platform.qq;
    public string? Lyric { get; set; }
    public string? Id { get; set; }
    public string? Trans { get; set; }
    public string? Romaji { get; set; }
}