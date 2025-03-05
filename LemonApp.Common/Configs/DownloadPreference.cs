using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.Common.Configs;
public class DownloadPreference
{
    public string? DefaultPath { get; set; }
    public MusicQuality PreferQuality { get; set; } = MusicQuality.SQ;
}
