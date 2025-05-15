namespace LemonApp.MusicLib.Abstraction.Entities;

public record class SingerPageData(Profile SingerProfile,
                                   string? BigBackground,
                                   List<AlbumInfo> RecentAlbums,
                                   List<AlbumInfo> Albums,
                                   List<Music> HotMusics,
                                   bool IsFollowed,
                                   List<Profile> SimilarSingers,
                                   long FansCount,
                                   SingerDesc Introduction);

public record class SingerDesc(string Desc,
                               Dictionary<string, string> Basic,
                               Dictionary<string, string> Other);