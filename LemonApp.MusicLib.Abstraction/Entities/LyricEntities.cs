namespace LemonApp.MusicLib.Abstraction.Entities;

public class LyricData
{
    public string? Lyric { get; set; }
    public string? Id { get; set; }
    public string? Trans { get; set; }
    public string? Romaji { get; set; }
}
public class LrcLine
{
    public string Lyric { get; set; } = string.Empty;
    public double Time { get; set; } = 0;
    public string? Trans { get; set; }
    public string? Romaji { get; set; }
}