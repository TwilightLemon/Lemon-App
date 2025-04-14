using System.Text.Json.Serialization;

namespace LemonApp.MusicLib.Abstraction.Entities;

public class MusicUrlData
{
    public string? Url, SourceText;
    public MusicQuality Quality;
}
public enum Platform
{
    qq, wyy,
    /// <summary>
    /// none means a  local file cannot match with any platform
    /// </summary>
    none
}
public class Profile
{
    public string Name { set; get; } = string.Empty;
    public string Photo { set; get; } = string.Empty;
    public string Mid { set; get; } = string.Empty;
}
public enum MusicQuality
{
    Std, HQ, SQ
}

public class Music
{
    public Platform Source { set; get; } = Platform.qq;
    public string MusicName { set; get; } = "";
    public string MusicName_Lyric { get; set; } = "";
    public List<Profile> Singer { set; get; } = [];
    public string SingerText { get; set; } = "";
    public string MusicID { set; get; } = "";
    public AlbumInfo? Album { set; get; }
    public string? Mvmid { set; get; }
    public MusicQuality Quality { set; get; }
    public string Littleid { set; get; } = "";
    public int MusicType { set; get; } = 0;
}