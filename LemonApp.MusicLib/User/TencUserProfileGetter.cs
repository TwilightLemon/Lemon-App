using LemonApp.MusicLib.Abstraction.UserAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.User
{
    public class TencUserProfileGetter : UserProfileGetter
    {
        public string? UserName { get; private set; } = null;

        public string? AvatarUrl { get; private set; } = null;

        public async Task Fetch<T>(T auth)
        {
            if(auth is TencUserAuth au)
            {
                await Task.Delay(200);
                UserName = "nihao";
                AvatarUrl = "./test.png";
            }
        }
    }
}
