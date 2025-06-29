using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Text.Json.Nodes;

namespace LemonApp.MusicLib.Home;

public static class PersonalityAPI
{
    public static async Task<PersonalityInfo> GetPersonality(HttpClient hc,TencUserAuth auth)
    {
        string data = await hc.SetCookie(auth.Cookie).GetStringAsync("https://i.y.qq.com/n2/m/client/portrait/index.html?_hidehd=1&_miniplayer=1&_fontscale=1");
        string json = TextHelper.FindTextByAB(data, "window.__ssrFirstPageData__=", "</script>", 0);

        var obj = JsonNode.Parse(json)["res"]["data"];

        string mainDesc = obj["MainDescription"]["Description"].ToString();
        List<Profile> singers = [];
        foreach(var singer in obj["Singers"].AsArray())
        {
            var pic = singer["Base"]["Pic"].ToString();
            var profile = new Profile
            {
                Mid = TextHelper.FindTextByAB(pic, "T001R150x150M000","_",0),
                Name = singer["Base"]["TypeTitle"].ToString(),
                Photo = pic
            };
            singers.Add(profile);
        }
        string personality = obj["Personality"]["RealMBTI"]["TypeTitle"].ToString();
        List<EmotionInfo> emotions = [];
        foreach(var emotion in obj["StatusIndex"]["Base"].AsArray())
        {
            var emotionInfo = new EmotionInfo(
                emotion["EnglishName"].ToString(),
                emotion["Num"].ToString(),
                emotion["ChangeNum"].ToString(),
                emotion["Pic"].ToString());
            emotions.Add(emotionInfo);
        }
        List<PreferenceInfo> preferences = [];
        foreach(var preference in obj["Genres"].AsArray())
        {
            var preferenceInfo = new PreferenceInfo(
                preference["Base"]["EnglishName"].ToString(),
                preference["Base"]["Slogan"].ToString());
            preferences.Add(preferenceInfo);
        }

        return new PersonalityInfo(mainDesc, singers, personality, emotions, preferences);
    }
}
