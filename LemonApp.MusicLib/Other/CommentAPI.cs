using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Abstraction.UserAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Other;

public static class CommentAPI
{
    public static async Task<List<Comment>> GetCommentsAsync(string mid,HttpClient hc,TencUserAuth auth)
    {
        return null;//不想写
    }
}
