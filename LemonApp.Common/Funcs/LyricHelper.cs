using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LemonApp.Common.Funcs;
public static class LyricHelper
{
    public static Dictionary<double, string?> Format(string text)
    {
        Regex regex = new Regex(@"\[(\d+):(\d+\.\d+)\](.*)");
        var lines = text.Split('\n');
        Dictionary<double, string?> result = [];
        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                var lyric = match.Groups[3].Value;
                if (string.IsNullOrWhiteSpace(lyric)) continue;
                //移除空歌词翻译
                if (lyric == "//") lyric = null;
                //移除末尾换行符(如果存在)
                if (lyric!=null&&lyric.EndsWith('\r'))
                    lyric = lyric[..^1];

                var minute = double.Parse(match.Groups[1].Value);
                var second = double.Parse(match.Groups[2].Value);
                var sec = minute * 60 + second;
                var millisecond = sec * 1000;
                result.Add(millisecond, lyric);
            }
        }
        return result;
    }
    public static bool IsJapanese(string text) 
        => Regex.Match(text, "[\u3040-\u309f]").Length > 0;
}
