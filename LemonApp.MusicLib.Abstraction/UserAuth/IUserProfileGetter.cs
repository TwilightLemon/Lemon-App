namespace LemonApp.MusicLib.Abstraction.UserAuth;

public interface IUserProfileGetter
{
    string? UserName { get; }
    string? AvatarUrl { get; }

    Task<bool> Fetch<T>(HttpClient client,T auth);
}
