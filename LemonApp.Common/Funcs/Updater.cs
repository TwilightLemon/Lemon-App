using System.IO;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace LemonApp.Common.Funcs;
public record class UpdaterConfig(Version NewestVersion,
                                  string Title,
                                  string Description,
                                  string DownloadUrl,
                                  string PublishDate,
                                  int ReleaseFileSize,
                                  string ReleasePage);
public class Updater
{
    public Updater(HttpClient httpClient)
    {
        hc = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        hc.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36");
    }
    private readonly HttpClient hc;
    private const string repo = "TwilightLemon/Lemon-App";
    private static string ReleaseFileName => Environment.Is64BitProcess ? "win-x64.zip" : "win-x86.zip";
    public UpdaterConfig? Config { get; private set; }
    public async Task<bool> CheckUpdateAsync(Version currentVersion)
    {
        try
        {
            string data = await hc.GetStringAsync($"https://api.github.com/repos/{repo}/releases/latest");
            if (string.IsNullOrEmpty(data))
                return false;
            var obj = JsonNode.Parse(data);
            var ver = obj?["tag_name"]?.ToString();
            var title = obj?["name"]?.ToString();
            var desc = obj?["body"]?.ToString();
            var date = obj?["created_at"]?.ToString();
            var release = obj?["assets"]?.AsArray()?.FirstOrDefault(x => x?["name"]?.ToString() == ReleaseFileName);
            var downloadUrl = release?["browser_download_url"]?.ToString();
            var filesize = release?["size"]?.GetValue<int>() ?? 0;
            var releasePage = obj?["html_url"]?.ToString() ?? "";
            if (string.IsNullOrEmpty(ver) || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(desc) || string.IsNullOrEmpty(date) || string.IsNullOrEmpty(downloadUrl))
                return false;

            Version version = Version.Parse(ver.TrimStart('v'));
            DateTimeOffset publishDate = DateTimeOffset.Parse(date);
            Config = new UpdaterConfig(version, title, desc, downloadUrl, publishDate.ToString("d"),filesize,releasePage);

            return version > currentVersion;
        }
        catch
        {
            return false;
        }
    }
}
