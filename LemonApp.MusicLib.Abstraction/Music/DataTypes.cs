using static LemonApp.MusicLib.Abstraction.Album.DataTypes;

namespace LemonApp.MusicLib.Abstraction.Music;
public static class DataTypes
{
    public class MusicUrlData
    {
        public string? Url, SourceText;
        public MusicQuality Quality;
    }
    public enum Platform
    {
        qq, wyy
    }
    public class Profile
    {
        public string Name { set; get; } = string.Empty;
        public string Photo { set; get; } = string.Empty;
        public string Mid { set; get; } = string.Empty;
    }
    public enum MusicQuality
    {
        _120k, HQ, SQ
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

        /// <summary>
        /// 相对于个人歌单里的id   用于“我喜欢”歌单
        /// </summary>
        public string? Littleid { set; get; }
    }
}