namespace LemonApp.MusicLib.Abstraction.Entities;
public record class HomePageEntities(List<Music> NewMusics,
                                     List<Playlist> Recommend,
                                     List<Playlist> Explore);