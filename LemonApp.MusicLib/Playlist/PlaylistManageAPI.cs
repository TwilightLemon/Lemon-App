using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;

namespace LemonApp.MusicLib.Playlist;

public static class PlaylistManageAPI
{
    public static async  Task<List<Abstraction.Entities.Playlist>> GetMyDissListAsync(
        HttpClient hc,TencUserAuth auth)
    {
        var param = new {
            uin=auth.Id,
            bWithoutStatus=false
        };
        var data = await (await hc.WrapForMusicu(auth, "music.musicasset.PlaylistBaseRead", "GetPlaylistByUin", param))
                        .Content.ReadAsStreamAsync();
        var json = (await JsonObject.ParseAsync(data))["req_1"]["data"]["v_playlist"].AsArray();
        var result = new List<Abstraction.Entities.Playlist>();
        foreach(var i in json)
        {
            result.Add(new() {
                Name = i["dirName"].ToString(),
                DirId = i["dirId"].ToString()
            });
        }
        return result;
    }
    internal record SongInfo(int songType,long songId);
    public static async Task<bool> WriteMusicsToMyDissAsync(
        HttpClient hc,TencUserAuth auth,string dirid,IList<Music> musics,bool delete=false)
    {
        if (dirid == null) return false;
        var param = new
        {
            dirId=int.Parse(dirid),
            v_songInfo=musics.Select(item=>new SongInfo(item.MusicType,long.Parse(item.Littleid))).ToList()
        };
        var data = await (await hc.WrapForMusicu(auth, "music.musicasset.PlaylistDetailWrite", delete? "DelSonglist" : "AddSonglist", param))
                        .Content.ReadAsStreamAsync();
        var json = await JsonObject.ParseAsync(data);
        return json["req_1"]["code"].ToString() == "0";
    }
}
