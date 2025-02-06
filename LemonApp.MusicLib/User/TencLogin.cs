using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.Http;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace LemonApp.MusicLib.User;

public static class TencLogin
{
    public static string  CalculateGtk(string p_skey)
    {
        long hash = 5381;
        foreach (char c in p_skey)
        {
            hash += (hash << 5) + c;
        }
        return (hash & 0x7fffffff).ToString();
    }
}
