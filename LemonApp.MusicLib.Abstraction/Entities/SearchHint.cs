using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.MusicLib.Abstraction.Entities;

public class SearchHint
{
    public static readonly SearchHint Empty = new();
    public enum HintType {Music,Album,Singer}
    public record Hint(string Content,string Id,string Singer,HintType Type);
    public List<Hint> Hints { get; } = [];
}
