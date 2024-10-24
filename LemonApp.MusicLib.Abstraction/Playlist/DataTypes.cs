using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.MusicLib.Abstraction.Playlist;

public static class DataTypes
{
    public class Playlist
    {
        public Profile? Creator { set; get; }
        public string Name { set; get; } = string.Empty;
        public string Photo { set; get; } = string.Empty;
        public string Id { set; get; } = string.Empty;
        public string? Description { set; get; }
        public string? Subtitle { set; get; }
        public List<Music.DataTypes.Music>? Musics { get; set; } = null;

        public string? DirId { get; set; }
        public List<string>? Ids { get; set; }
        public bool IsOwner { get; set; } = false;
        public bool IsFavorite { get; set; } = false;
    }
}
