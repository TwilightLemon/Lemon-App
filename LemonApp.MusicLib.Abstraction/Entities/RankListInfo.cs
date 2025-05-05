namespace LemonApp.MusicLib.Abstraction.Entities;
public record class RankListInfo(string Name,
                           string CoverUrl,
                           string Id,
                           string Description,
                           List<string> Content);
