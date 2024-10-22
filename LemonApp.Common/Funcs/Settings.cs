﻿using System.Diagnostics;
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
    public T? Data { get; set; }
    [JsonIgnore]
    private FileSystemWatcher? _watcher;
    /// <summary>
    /// 监测到配置文件改变时触发，之前不会自动更新数据
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
    public SettingsMgr(string Sign, string pkgName)
    {
        this.Sign = Sign;
        this.PackageName = pkgName;
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
    public async Task<bool> Load()
    {
        if (Sign is null) return false;
        try
        {
            Debug.WriteLine($"SettingsMgr<{typeof(T).Name}> {Sign} Start to Load");
            var dt = await Settings.Load<SettingsMgr<T>>(Sign, Settings.sType.Settings);
            if (dt != null)
                Data = dt.Data;
            else
            {
                Data = Activator.CreateInstance<T>();
                await Save();
            }
            return true;
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"SettingsMgr<{typeof(T).Name}> {Sign} Load failed: {ex.Message}");
            return false;
        }
    }
    public async Task Save()
    {
        if (Sign is null || _watcher is null) return;

        Debug.WriteLine($"SettingsMgr<{typeof(T).Name}> {Sign} Start to Save");
        _watcher.EnableRaisingEvents = false;
        await Settings.Save(this, Sign, Settings.sType.Settings);
        _watcher.EnableRaisingEvents = true;
    }
    private DateTime _lastUpdateTime = DateTime.MinValue;
    private void _watcher_Changed(object sender, FileSystemEventArgs e)
    {
        if (DateTime.Now - _lastUpdateTime > TimeSpan.FromSeconds(1))
        {
            Debug.WriteLine($"SettingsMgr<{typeof(T).Name}> {Sign} file Changed");
            OnDataChanged?.Invoke();
            _lastUpdateTime = DateTime.Now;
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
    public static async Task Save<T>(T Data, string Sign, sType type) where T : class
    {
        try
        {
            string path = GetPathBySign(Sign, type);
            await SaveAsJsonAsync<T>(Data, path);
        }
        catch { }
    }
    public static async Task SaveAsJsonAsync<T>(T Data,string path) where T : class
    {
        var fs = File.Create(path);
        await JsonSerializer.SerializeAsync<T>(fs, Data, _options);
        fs.Close();
    }
    public static async Task<T?> Load<T>(string Sign, sType t) where T : class
    {
        string path = GetPathBySign(Sign, t);
        var data = await LoadFromJsonAsync<T>(path);
        return data;
    }

    public static async Task<T?> LoadFromJsonAsync<T>(string path) where T : class
    {
        if (!File.Exists(path))
            return null;
        var fs = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<T>(fs, _options);
        fs.Close();
        return data;
    }
}
