﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Abstraction.Entities;

public class AlbumInfo
{
    public Profile? Creator { set; get; }
    public string Name { set; get; } = string.Empty;
    public string Photo { set; get; } = string.Empty;
    public string Id { set; get; } = string.Empty;
    public string? Description { set; get; }
    public List<Music>? Musics { get; set; } = null;
}