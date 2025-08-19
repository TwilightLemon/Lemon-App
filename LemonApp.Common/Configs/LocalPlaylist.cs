using LemonApp.MusicLib.Abstraction.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LemonApp.Common.Configs;
public record class LocalDirMeta(string Path, string Name);
public class LocalPlaylist
{
    public List<LocalDirMeta> LocalDirs { get; set; } = [];
    /// <summary>
    /// file path hash -> music map
    /// </summary>
    public Dictionary<string, Music> IdMap { get; set; } = [];
}
