using LemonApp.MusicLib.Abstraction.Playlist;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.MusicLib.User;
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
#pragma warning disable CS8602 // 解引用可能出现空引用。
public class TencUserProfileGetter
{
    public string? UserName { get; private set; } = null;

    public string? AvatarUrl { get; private set; } = null;

    public DataTypes.Playlist? MyFavorite { get; private set; } = null;
    public List<DataTypes.Playlist> MyPlaylists { get; private set; } = [];

    public async Task<bool> Fetch<T>(HttpClient client,T auth)
    {
        if(auth is TencUserAuth{Id:not null,Cookie:not null} au)
        {
            try
            {
                string qq = au.Id;
                string url = $"https://c.y.qq.com/rsc/fcgi-bin/fcg_get_profile_homepage.fcg?loginUin={qq}&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0&cid=205360838&ct=20&userid={qq}&reqfrom=1&reqtype=0";
                string data = await (await client.SetForCYQ(au.Cookie)
                                            .GetAsync(url)).Content.ReadAsStringAsync();
                if (JsonNode.Parse(data) is { } json)
                {
                    var per = json["data"]?["creator"];
                    if (per != null)
                    {
                        //获取个人信息
                        UserName = per["nick"].ToString();
                        AvatarUrl = per["headpic"].ToString().Replace("http://", "https://");
                        var shown = new Profile() { Name = UserName, Photo = AvatarUrl };

                        //获取 我喜欢 Playlist
                        var mymusic = json["data"]?["mymusic"].AsArray();
                        var id = mymusic.First(p => p["type"].ToString() == "1" && p["title"].ToString()=="我喜欢")["id"].ToString();
                        MyFavorite = new DataTypes.Playlist()
                        {
                            Id = id,
                            DirId = "201",
                            Name = "我喜欢",
                            IsOwner = true,
                            Photo = "",
                            Creator = shown,
                        };

                        //获取 我创建的歌单
                        MyPlaylists.Clear();
                        var myplaylist = json["data"]?["mydiss"]["list"].AsArray();
                        foreach ( var item in myplaylist)
                        {
                            var pl = new DataTypes.Playlist()
                            {
                                Id = item["dissid"].ToString(),
                                Name = item["title"].ToString(),
                                Photo = item["picurl"].ToString(),
                                Creator =shown,
                                DirId = item["dirid"].ToString(),
                                IsOwner = true,
                                Subtitle = item["subtitle"].ToString(),
                                //Description 在这里没有给出，加载歌单时候再获取
                            };
                            MyPlaylists.Add(pl);
                        }
                        
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
        return false;
    }
}
