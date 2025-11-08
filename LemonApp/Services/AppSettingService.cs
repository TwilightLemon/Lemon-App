using LemonApp.Common;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LemonApp.Services;
/// <summary>
/// Manage user settings.
/// </summary>
public class AppSettingService : IHostedService, IConfigManager
{
    private readonly Dictionary<Type, ISettingsMgr> _settingsMgrs = [];
    private readonly TimeSpan SaveInterval = TimeSpan.FromMinutes(5);
    private readonly string _pkgName = typeof(AppSettingService).Namespace!;
    private Timer? _timer;
    public event Action? OnDataSaving;

    public AppSettingService AddConfig<T>(Settings.sType type = Settings.sType.Settings) where T : class
    {
        var instance = new SettingsMgr<T>(typeof(T).Name, _pkgName, type);
        if (instance is ISettingsMgr { } mgr)
            _settingsMgrs.Add(typeof(T), mgr);
        return this;
    }

    public SettingsMgr<T> GetConfigMgr<T>() where T : class
    {
        if (_settingsMgrs.TryGetValue(typeof(T), out var mgr))
            return (SettingsMgr<T>)mgr;
        throw new InvalidOperationException($"{typeof(T)} is not registered.");
    }
    public bool AddEventHandler<T>(Action handler) where T : class
    {
        if (_settingsMgrs.TryGetValue(typeof(T), out var mgr))
        {
            mgr.OnDataChanged += handler;
            return true;
        }
        return false;
    }
    private void Load()
    {
        GlobalConstants.ConfigManager = this;
        foreach (var mgr in _settingsMgrs.Values)
        {
            if (!mgr.Load())
                throw new Exception($"failed to load AppSettings: {mgr}");
        }
    }
    private void Save()
    {
        try
        {
            OnDataSaving?.Invoke();
        }
        finally
        {
            OnDataSaving = null;
            foreach (var mgr in _settingsMgrs.Values)
            {
                try
                {
                    mgr.Save();
                    Debug.WriteLine($"SettingsMgr<{mgr}> Saved");
                }
                catch
                {
                    Debug.WriteLine($"SettingsMgr<{mgr}> failed to save");
                    continue;
                }
            }
        }
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Load();
        _timer = new Timer(_ => Save(), null, SaveInterval, SaveInterval);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Save();
        _timer?.Dispose();
        return Task.CompletedTask;
    }
}
