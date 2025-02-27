using LemonApp.MusicLib.Abstraction.Entities;

namespace LemonApp.Common.Configs;

public record class DownloadItem(string FileFullPath,string DisplayName,string Mid,Platform Platform);
public class DownloadServiceCache
{
    public string? DefaultPath = null;
    public IList<DownloadItem> History = [];
}
