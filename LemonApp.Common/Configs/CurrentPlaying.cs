using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MusicDT = LemonApp.MusicLib.Abstraction.Music.DataTypes;

namespace LemonApp.Common.Configs;

public class CurrentPlaying
{
    public MusicDT.Music? Music { get; set; }
    public MusicDT.MusicQuality Quality { get; set; }
    public double Volume { get; set; }
}
