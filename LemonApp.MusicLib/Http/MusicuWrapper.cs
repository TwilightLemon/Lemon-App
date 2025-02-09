using LemonApp.MusicLib.Abstraction.UserAuth;
using System.Net.Http.Json;
using System.Text.Json;

namespace LemonApp.MusicLib.Http;

public static class MusicuWrapper
{
    public static readonly string MusicuUrl = "https://u.y.qq.com/cgi-bin/musicu.fcg";
    public static Task<HttpResponseMessage> WrapForMusicu(this HttpClient hc,TencUserAuth auth,string Module,string Method,object Param)
    {
        var json = new Root()
        {
            comm = new(long.Parse(auth.Id), long.Parse(auth.G_tk)),
            req_1=new Request(Module, Method)
            {
                param=Param
            }
        };
        return hc.SetForMusicuFcg(auth.Cookie).PostAsJsonAsync(MusicuUrl,json);
    }

    public class Comm(long userId,long gtk)
    {
        public int cv { get; set; } = 4747474;
        public int ct { get; set; } = 24;
        public string format { get; set; } = "json";
        public string inCharset { get; set; }= "utf-8";
        public string outCharset { get; set; } = "utf-8";
        public int notice { get; set; } = 0;
        public string platform { get; set; } = "yqq.json";
        public int needNewCode { get; set; } = 1;
        public long uin { get; set; }= userId;
        public long g_tk_new_20200303 { get; set; } = gtk;
        public long g_tk { get; set; } = gtk;
    }

    public class Request(string Module,string Method)
    {
        public string module { get; set; } = Module;
        public string method { get; set; } = Method;
        public object? param { get; set; }
    }

    public class Root
    {
        public Comm comm { get; set; }
        public Request req_1 { get; set; }
    }

}
