namespace LemonApp.MusicLib.Abstraction.Entities;

public record class SingerPageData(Profile SingerProfile,
                                   List<AlbumInfo> RecentAlbums,
                                   List<Music> HotMusics,
                                   bool IsFollowed,
                                   string Description,
                                   string FansCount);