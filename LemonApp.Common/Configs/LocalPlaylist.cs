using LemonApp.MusicLib.Abstraction.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LemonApp.Common.Configs;
public class LocalDirMeta
{
    public string? Path { get; set; }
    public string? Name { get; set; }
    /// <summary>
    /// filename - metadata
    /// </summary>
    public Dictionary<string, Music>? Musics;
}
public class LocalPlaylist
{
    public List<LocalDirMeta> LocalDirs { get; set; } = [];
}
