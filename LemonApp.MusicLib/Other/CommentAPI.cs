using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Other;

public static class CommentAPI
{
    public static async Task<CommentPageData> GetCommentsAsync(string mid,HttpClient hc,TencUserAuth auth)
    {
        string id = JObject.Parse(await hc.GetStringAsync($"https://c.y.qq.com/v8/fcg-bin/fcg_play_single_song.fcg?songmid={mid}&tpl=yqq_song_detail&format=json&g_tk={auth.G_tk}&loginUin={auth.Id}&hostUin=0&format=json&inCharset=utf8&outCharset=utf-8&notice=0&platform=yqq&needNewCode=0"))["data"][0]["id"].ToString();
        var da = await (await hc.SetForMusicuFcg(auth.Cookie).PostTextAsync("https://u.y.qq.com/cgi-bin/musicu.fcg",
"{\"req_0\":{\"method\":\"GetNewCommentList\",\"module\":\"music.globalComment.CommentReadServer\",\"param\":{\"BizType\":1,\"BizId\":\"" + id + "\",\"PageSize\":20,\"PageNum\":0,\"WithHot\":1}},\"req_1\":{\"method\":\"GetHotCommentList\",\"module\":\"music.globalComment.CommentReadServer\",\"param\":{\"BizType\":1,\"BizId\":\"" + id + "\",\"LastCommentSeqNo\":null,\"" +
"PageSize\":10,\"PageNum\":0,\"HotType\":2,\"WithAirborne\":1}},\"comm\":{\"g_tk\":" + auth.G_tk + ",\"uin\":\"" + auth.Id + "\",\"format\":\"json\",\"ct\":20,\"cv\":1773,\"platform\":\"wk_v17\"}}")).AsTextAsync();
        JObject ds = JObject.Parse(da);
        var main = ds["req_0"]["data"];
        //---------最近热评-----
        List<Comment> Present = [];
        foreach (var a in main["CommentList3"]["Comments"])
        {
            Present.Add(BuildComment(a));
        }
        //---------精彩评论-----
        List<Comment> Hot = [];
        foreach (var a in main["CommentList2"]["Comments"])
        {
            Hot.Add(BuildComment(a));
        }
        //---------最新评论-----
        List<Comment> Now = [];
        foreach (var a in main["CommentList"]["Comments"])
        {
            Now.Add(BuildComment(a));
        }
        return new(Present, Hot, Now);
    }

    private static Comment BuildComment(JToken a)
    {
        DateTime dtStart = TimeZoneInfo.ConvertTime(new DateTime(1970, 1, 1), TimeZoneInfo.Local);
        long lTime = long.Parse(a["PubTime"].ToString() + "0000000");
        TimeSpan toNow = new TimeSpan(lTime);
        DateTime daTime = dtStart.Add(toNow);
        var time = daTime.ToString("yyyy-MM-dd  HH:mm");
        var isLiked = a["IsPraised"].ToString() == "1";
        return new(a["Nick"].ToString(),
                   a["Avatar"].ToString(),
                   TextHelper.Exem(a["Content"].ToString().Replace(@"\n", "\n").Replace("[图片]", "")),
                   a["PraiseNum"].ToString(),
                   time,
                   a["CmId"].ToString(),
                   isLiked);
    }
}
