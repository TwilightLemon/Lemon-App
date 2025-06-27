namespace LemonApp.MusicLib.Abstraction.Entities;

public enum LyricType
{
    QQ,Wyy,PureWyy
}
public class LyricData
{
    public LyricType Type { get; set; }
    public string? Lyric { get; set; }
    public string? Id { get; set; }
    public string? Trans { get; set; }
    public string? Romaji { get; set; }
}