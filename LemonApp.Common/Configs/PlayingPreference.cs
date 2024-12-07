﻿using MusicDT = LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.Common.Configs;
public class PlayingPreference
{
    public enum CircleMode {Circle,Single,Random}
    public MusicDT.Music? Music { get; set; }
    public MusicDT.MusicQuality Quality { get; set; } = MusicDT.MusicQuality.SQ;
    public double Volume { get; set; }
    public CircleMode PlayMode { get; set; }= CircleMode.Circle;
    public bool ShowDesktopLyric { get; set; } = false;
}
public class PlaylistCache
{
    public List<MusicDT.Music>? Playlist { get; set; } = [];
}
public class DesktopLyricOption
{
    public bool ShowTranslation { get; set; } = true;
}