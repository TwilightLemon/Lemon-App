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
public class AppSettingService :IHostedService,IConfigManager
{
    private readonly Dictionary<Type, object> _settingsMgrs = [];
    public event Action? OnExiting;

    public AppSettingService AddConfig<T>(Settings.sType type=Settings.sType.Settings) where T : class{
        if(Activator.CreateInstance(typeof(SettingsMgr<>).MakeGenericType(typeof(T)),
        [typeof(T).Name,typeof(AppSettingService).Namespace,type]) is {} mgr)
            _settingsMgrs.Add(typeof(T),mgr);
        return this;
    }

    public SettingsMgr<T> GetConfigMgr<T>() where T : class{
        if(_settingsMgrs.TryGetValue(typeof(T),out var mgr))
            return (SettingsMgr<T>)mgr;
        throw new InvalidOperationException($"{typeof(T)} is not registered.");
    }
    public bool AddEventHandler<T>(Action handler) where T : class
    {
        if(_settingsMgrs.TryGetValue(typeof(T), out var mgr))
        {
            if(mgr.GetType().GetEvent("OnDataChanged") is { } info)
            {
                info.AddEventHandler(mgr, handler);
                return true;
            }
        }
        return false;
    }
    public void Load(){
        GlobalConstants.ConfigManager = this;
        foreach (var mgr in _settingsMgrs.Values)
        {
            var loadMethod = mgr.GetType().GetMethod("Load");
            if (loadMethod?.Invoke(mgr, null) is false)
                throw new Exception($"failed to load AppSettings: {mgr}");
        }
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Load();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            OnExiting?.Invoke();
        }
        finally
        {
            foreach (var mgr in _settingsMgrs.Values)
            {
                var saveMethod = mgr.GetType().GetMethod("Save");
                saveMethod?.Invoke(mgr, null);
                Debug.WriteLine($"SettingsMgr<{mgr.GetType().GenericTypeArguments[0].Name}> Saved");
            }
        }
        return Task.CompletedTask;
    }
}
