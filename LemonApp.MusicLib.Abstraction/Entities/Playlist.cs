namespace LemonApp.MusicLib.Abstraction.Entities;

public enum PlaylistType
{
    Album, Playlist, Ranklist, Other
}
public class Playlist
{
    public Platform Source { set; get; } = Platform.qq;
    public Profile? Creator { set; get; }
    public string Name { set; get; } = string.Empty;
    public string Photo { set; get; } = string.Empty;
    public string Id { set; get; } = string.Empty;
    public string? Description { set; get; }
    public string? Subtitle { set; get; }
    public List<Music>? Musics { get; set; } = null;

    public string? DirId { get; set; }
    public bool IsOwner { get; set; } = false;
}
