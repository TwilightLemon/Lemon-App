using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.User
{
    public class TencUserProfileGetter : IUserProfileGetter
    {
        public string? UserName { get; private set; } = null;

        public string? AvatarUrl { get; private set; } = null;

        public async Task<bool> Fetch<T>(HttpClient client,T auth)
        {
            if(auth is TencUserAuth{Id:not null,Cookie:not null} au)
            {
                string qq = au.Id;
                string url = $"https://c.y.qq.com/rsc/fcgi-bin/fcg_get_profile_homepage.fcg?loginUin={qq}&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0&cid=205360838&ct=20&userid={qq}&reqfrom=1&reqtype=0";
                string data = await  (await client.SetForCYQ(au.Cookie)
                                            .GetAsync(url)).Content.ReadAsStringAsync();
                if(JsonNode.Parse(data) is { } json)
                {
                    var per=json["data"]?["creator"];
                    if(per != null)
                    {
                        UserName = per["nick"].ToString();
                        AvatarUrl = per["headpic"].ToString().Replace("http://", "https://");
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
