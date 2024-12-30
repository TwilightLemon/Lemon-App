using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace LemonApp.Common.Funcs;
/// <summary>
/// 统一的缓存和配置类
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class SettingsMgr<T> where T : class
{
    public string? Sign { get; set; }
    public string? PackageName { get; set; }
    public T Data { get; set; } = null;
    [JsonIgnore]
    private FileSystemWatcher? _watcher;
    [JsonIgnore]
    private Settings.sType _type= Settings.sType.Settings;
    /// <summary>
    /// 监测到配置文件改变时触发，之前会自动更新数据
    /// </summary>
    public event Action? OnDataChanged;
    /// <summary>
    /// 为json序列化保留的构造函数
    /// </summary>
    public SettingsMgr() { }
    static SettingsMgr()
    {
        Settings.LoadPath();
    }
    public SettingsMgr(string Sign, string pkgName,Settings.sType type=Settings.sType.Settings)
    {
        this.Sign = Sign;
        this.PackageName = pkgName;
        this._type = type;
        _watcher = new FileSystemWatcher(Settings.SettingsPath)
        {
            Filter = Sign + ".json",
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        _watcher.Changed += _watcher_Changed;
    }
    ~SettingsMgr()
    {
        _watcher?.Dispose();
    }
    public bool Load()
    {
        if(Sign is null)return false;
        try
        {
            var dt = Settings.Load<SettingsMgr<T>>(Sign, _type);
            if (dt != null)
            {
                Data = dt.Data;
            }
            else
            {
                Data = Activator.CreateInstance<T>();
                Save();
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LoadAsync()
    {
        if (Sign is null) return false;
        try
        {
            Debug.WriteLine($"SettingsMgr<{typeof(T).Name}> {Sign} Start to Load");
            var dt = await Settings.LoadAsync<SettingsMgr<T>>(Sign, _type);
            if (dt != null)
                Data = dt.Data;
            else
            {
                Data = Activator.CreateInstance<T>();
                await SaveAsync();
            }
            return true;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"SettingsMgr<{typeof(T).Name}> {Sign} Load failed: {ex.Message}");
            return false;
        }
    }
    public async Task SaveAsync()
    {
        if (Sign is null || _watcher is null) return;

        Debug.WriteLine($"SettingsMgr<{typeof(T).Name}> {Sign} Start to Save");
        _watcher.EnableRaisingEvents = false;
        await Settings.SaveAsync(this, Sign, _type);
        _watcher.EnableRaisingEvents = true;
    }

    public void Save()
    {
        if (Sign is null || _watcher is null) return;

        Debug.WriteLine($"SettingsMgr<{typeof(T).Name}> {Sign} Start to Save");
        _watcher.EnableRaisingEvents = false;
         Settings.Save(this, Sign, _type);
        _watcher.EnableRaisingEvents = true;
    }

    private DateTime _lastUpdateTime = DateTime.MinValue;
    private async void _watcher_Changed(object sender, FileSystemEventArgs e)
    {
        await Task.Delay(300);
        if (DateTime.Now - _lastUpdateTime > TimeSpan.FromSeconds(1))
        {
            _lastUpdateTime = DateTime.Now;
            Debug.WriteLine($"SettingsMgr<{typeof(T).Name}> {Sign} file Changed");
            await LoadAsync();
            OnDataChanged?.Invoke();
        }
    }
}
public static class Settings
{

    private const string RootName = "LemonAppNew";
    private static JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
    public enum sType { Cache, Settings }
    public static string MainPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), RootName);
    public static string CachePath =>
        Path.Combine(MainPath, "Cache");
    public static string SettingsPath =>
        Path.Combine(MainPath, "Settings");
    public static void LoadPath()
    {
        if (!Directory.Exists(MainPath))
            Directory.CreateDirectory(MainPath);
        if (!Directory.Exists(CachePath))
            Directory.CreateDirectory(CachePath);
        if (!Directory.Exists(SettingsPath))
            Directory.CreateDirectory(SettingsPath);
    }
    public static string GetPathBySign(string Sign, sType type) => Path.Combine(type switch
    {
        sType.Cache => CachePath,
        sType.Settings => SettingsPath,
        _ => throw new NotImplementedException()
    }, Sign + ".json");
    public static async Task SaveAsync<T>(T Data, string Sign, sType type) where T : class
    {
        try
        {
            string path = GetPathBySign(Sign, type);
            await SaveAsJsonAsync(Data, path, type == sType.Settings);
        }
        catch { }
    }
    public static void Save<T>(T Data,string Sign, sType type)where T : class
    {
        try
        {
            string path = GetPathBySign(Sign, type);
            SaveAsJson(Data, path, type == sType.Settings);
        }
        catch { }
    }
    public static void SaveAsJson<T>(T Data, string path, bool useOptions = true) where T : class
    {
        var fs = File.Create(path);
        JsonSerializer.Serialize<T>(fs, Data, useOptions ? _options : null);
        fs.Close();
    }
    public static async Task SaveAsJsonAsync<T>(T Data,string path,bool useOptions=true) where T : class
    {
        var fs = File.Create(path);
        await JsonSerializer.SerializeAsync<T>(fs, Data, useOptions?_options:null);
        fs.Close();
    }
    public static async Task<T?> LoadAsync<T>(string Sign, sType t) where T : class
    {
        string path = GetPathBySign(Sign, t);
        var data = await LoadFromJsonAsync<T>(path, t == sType.Settings);
        return data;
    }

    public static T? Load<T>(string Sign,sType t)where T : class
    {
        string path = GetPathBySign(Sign, t);
        var data = LoadFromJson<T>(path, t == sType.Settings);
        return data;
    }

    public static async Task<T?> LoadFromJsonAsync<T>(string path,bool useOptions=true) where T : class
    {
        if (!File.Exists(path))
            return null;
        var fs = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<T>(fs, useOptions ? _options : null);
        fs.Close();
        return data;
    }

    public static T? LoadFromJson<T>(string path, bool useOptions = true) where T : class
    {
        if (!File.Exists(path))
            return null;
        var fs = File.OpenRead(path);
        var data = JsonSerializer.Deserialize<T>(fs, useOptions ? _options : null);
        fs.Close();
        return data;
    }
}
