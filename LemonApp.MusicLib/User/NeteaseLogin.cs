using LemonApp.MusicLib.Abstraction.UserAuth;
using System.Text.RegularExpressions;

namespace LemonApp.MusicLib.User;
public class NeteaseLogin(HttpClient hc)
{
    public event Action<NeteaseUserAuth>? OnAuthCompleted;
    public void CollectInfo(string html,string cookie)
    {
        var regex = Regex.Match(html, @"(?<=/user/home\?id=)\d+");
        if (regex.Success)
        {
            string id = regex.Value;
            OnAuthCompleted?.Invoke(new NeteaseUserAuth() { Id = id,Cookie=cookie });
        }
    }
}
