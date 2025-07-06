using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.Common.Configs;

public enum QuickAccessType
{
    Playlist,
    Album,
    Artist,
    RankList
}
public class QuickAccess
{
    public QuickAccessType Type { get; set; }
    public string Title { get; set; }
    public string Id { get; set; }
    public string CoverUrl { get; set; }
    public Platform Platform { get; set; }
    //reserved for json deserialization
    public QuickAccess() { }
    public QuickAccess(QuickAccessType type, string title, string id, string coverUrl, Platform platform = Platform.qq)
    {
        Type = type;
        Title = title;
        Id = id;
        CoverUrl = coverUrl;
        Platform = platform;
    }

    public QuickAccess(Playlist playlist) : this(QuickAccessType.Playlist,playlist.Name, playlist.Id, playlist.Photo, playlist.Source) { }
    public QuickAccess(AlbumInfo album) : this(QuickAccessType.Album,album.Name, album.Id, album.Photo) { }
    public QuickAccess(Profile artist) : this(QuickAccessType.Artist,artist.Name, artist.Mid, artist.Photo) { }
    public QuickAccess(RankListInfo rank) : this(QuickAccessType.RankList,rank.Name, rank.Id, rank.CoverUrl) { }
}

public class QuickAccessCollection
{
    public List<QuickAccess> Items { get; set; } = [];
}