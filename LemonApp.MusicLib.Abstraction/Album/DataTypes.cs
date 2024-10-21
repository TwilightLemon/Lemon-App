using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.MusicLib.Abstraction.Album;

public static class DataTypes
{
    public class AlbumInfo
    {
        public Platform Source { set; get; } = Platform.qq;
        public Profile? Creator { set; get; }
        public string Name { set; get; } = string.Empty;
        public string Photo { set; get; } = string.Empty;
        public string Id { set; get; } = string.Empty;
        public string? Description { set; get; }
        public List<Music.DataTypes.Music>? Musics { get; set; } = null;
    }
}
