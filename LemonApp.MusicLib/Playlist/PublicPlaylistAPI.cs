using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;
using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.MusicLib.Playlist;
#pragma warning disable CS8602 // 解引用可能出现空引用。
public static class PublicPlaylistAPI
{
    public static async Task<Abstraction.Entities.Playlist?> LoadPlaylistById(HttpClient hc, TencUserAuth auth, string id)
    {
        string url = $"https://c.y.qq.com/qzone/fcg-bin/fcg_ucc_getcdinfo_byids_cp.fcg?type=1&json=1&utf8=1&onlysong=0&disstid={id}&format=json&g_tk={auth.G_tk}&loginUin={auth.Id}&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0";
        string data = await hc.SetForCYQ(auth.Cookie)
                                .GetStringAsync(url);
        if (JsonNode.Parse(data) is { } json && json["cdlist"]?.AsArray()[0] is { } diss)
        {
            var pl = new Abstraction.Entities.Playlist();
            pl.Id = id;
            pl.Name = diss["dissname"].ToString();
            pl.Photo = diss["logo"].ToString().Replace("http://", "https://");
            pl.Creator = new Profile()
            {
                Name = diss["nick"].ToString(),
                Photo = diss["headurl"].ToString()
            };
            pl.Ids = diss["songids"].ToString().Split(',').ToList();
            pl.IsOwner = diss["login"].ToString() == diss["uin"].ToString();
            pl.Description = diss["desc"].ToString();
            pl.Musics = [];

            var list = diss["songlist"].AsArray();
            int index = 0;
            foreach (var item in list)
            {
                string songtype = item["songtype"].ToString();

                var singers = new List<Profile>();
                var singerlist = item["singer"].AsArray();
                var sl = new List<string>();
                foreach (var singer in singerlist)
                {
                    string name = singer["name"].ToString();
                    singers.Add(new Profile()
                    {
                        Name = name,
                        Mid = singer["mid"].ToString()
                    });
                    sl.Add(name);
                }
                var singerText = string.Join("/", sl);

                var m = new Music();
                m.MusicName = item["songname"].ToString();
                m.SingerText = singerText;
                m.Singer = singers;
                if (songtype == "0")
                {
                    m.MusicName_Lyric = item["albumdesc"].ToString();
                    m.MusicID = item["songmid"].ToString();
                    var amid = item["albummid"].ToString();
                    if (amid != "")
                        m.Album = new AlbumInfo()
                        {
                            Id = amid,
                            Photo = $"https://y.gtimg.cn/music/photo_new/T002R500x500M000{amid}.jpg?max_age=2592000",
                            Name = item["albumname"].ToString()
                        };
                    m.Mvmid = item["vid"].ToString();
                    m.Littleid = pl.Ids[index];

                    m.Quality = item["sizeflac"].ToString() != "0" ? MusicQuality.SQ : (item["size320"].ToString() != "0" ? MusicQuality.HQ : MusicQuality.Std);
                }

                pl.Musics.Add(m);
                index++;
            }
            return pl;
        }
        return null;
    }
}
