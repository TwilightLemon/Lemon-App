using LemonApp.MusicLib.Abstraction.UserAuth;

namespace LemonApp.Common.Configs;

public class UserProfile{
    public string? UserName { get; set; }
    public string? AvatarUrl { get; set; }
    public TencUserAuth? TencUserAuth { get; set; }
    public NeteaseUserAuth? NeteaseUserAuth { get; set; }
}