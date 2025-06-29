using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Abstraction.Entities;

public record class PersonalityInfo(string MainDescription,
                                    List<Profile> Singers,
                                    string MusicPersonality,
                                    List<EmotionInfo> Emotions,
                                    List<PreferenceInfo> Preferences);

public record class PreferenceInfo(string Type,string Slogan);
public record class EmotionInfo(string Name,string Num,string Delta,string Pic);
