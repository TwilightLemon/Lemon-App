namespace LemonApp.MusicLib.Abstraction.Entities;

public class AlbumInfo
{
    public Profile? Creator { set; get; }
    public string Name { set; get; } = string.Empty;
    public string Photo { set; get; } = string.Empty;
    public string Id { set; get; } = string.Empty;
    public string? Description { set; get; }
    public string? PublishDate { get; set; }
    public List<Music>? Musics { get; set; } = null;
    public Platform Platform { set; get; } = Platform.qq;
}
