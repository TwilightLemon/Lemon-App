using System.IO;

namespace LemonApp.Common.Funcs;
public static class CacheManager
{
    private const string RootName= "LemonApp";
    public enum CacheType
    {
        Music,Lyric,Other
    }
    private static SettingsMgr<CacheSettings> _settingsMgr =
        new(typeof(CacheManager).Name, typeof(CacheManager).Namespace!);
    private static string GetTypeName(CacheType type) => type switch {
        CacheType.Music=>"Music",
        CacheType.Lyric=>"Lyric",
        _=>"Other"
    };
    private static string GetDefaultCachePath()
    {
        DriveInfo[] allDirves = DriveInfo.GetDrives();
        string dir = "C:\\";
        foreach (DriveInfo item in allDirves)
        {
            if (item.DriveType == DriveType.Fixed && item.Name != "C:\\")
            {
                dir = item.Name;
                break;
            }
        }
        return Path.Combine(dir, RootName);
    }
    public static  async Task LoadPath()
    {
        await  _settingsMgr.Load();
        if (_settingsMgr.Data?.CachePath == null)
        {
            CacheSettings paths = new();
            paths.CachePath = GetDefaultCachePath();
            _settingsMgr.Data = paths;
            await _settingsMgr.Save();
        }
        foreach(CacheType type in Enum.GetValues(typeof(CacheType)))
        {
            var path=Path.Combine(_settingsMgr.Data.CachePath,GetTypeName(type));
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
    public static string GetCachePath(CacheType type) =>
        Path.Combine(_settingsMgr.Data?.CachePath??throw new InvalidOperationException("CacheManager didn't load yet.")
            ,GetTypeName(type));
}

internal class CacheSettings
{
    public string? CachePath { get; set; } = null;
}
