using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Abstraction.UserAuth;

public interface UserProfileGetter
{
    string? UserName { get; }
    string? AvatarUrl { get; }

    Task Fetch<T>(T auth);
}
