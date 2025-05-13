namespace LemonApp.MusicLib.Abstraction.Entities;

public record class SingerPageData(Profile SingerProfile,
                                   List<AlbumInfo> RecentAlbums,
                                   List<Music> HotMusics,
                                   bool IsFollowed,
                                   List<Profile> SimilarSingers,
                                   string FansCount,
                                   SingerDesc Introduction);

public record class SingerDesc(string Desc,
                               Dictionary<string, string> Basic,
                               Dictionary<string, string> Other);