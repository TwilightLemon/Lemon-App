using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.Common.Configs;
public class PlayingPreference
{
    public enum CircleMode {Circle,Single,Random}
    public Music? Music { get; set; }
    public MusicQuality Quality { get; set; } = MusicQuality.SQ;
    public double Volume { get; set; }
    public CircleMode PlayMode { get; set; }= CircleMode.Circle;
    public bool ShowDesktopLyric { get; set; } = false;
    public bool EnableEmbededLyric { get; set; } = false;
}
public class PlaylistCache
{
    public List<Music>? Playlist { get; set; } = [];
}
public class DesktopLyricOption
{
    public bool ShowTranslation { get; set; } = true;
}

public class LyricOption
{
    public bool ShowTranslation { get; set; } = true;
    public bool ShowRomaji { get; set; } = true;
    public int FontSize { get; set; } = 24;
    public string FontFamily { get; set; } = "Segou UI";
}