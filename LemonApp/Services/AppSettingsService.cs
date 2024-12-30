using LemonApp.Common;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LemonApp.Services;
/// <summary>
/// Manage user settings.
/// </summary>
/// <param name="logger"></param>
public class AppSettingsService(
    ILogger<AppSettingsService> logger
    ) :IHostedService,IConfigManager
{
    private readonly Dictionary<Type, object> _settingsMgrs = [];
    private readonly ILogger<AppSettingsService> _logger = logger;
    public event Action? OnExiting;

    public AppSettingsService AddConfig<T>(Settings.sType type=Settings.sType.Settings) where T : class{
        if(Activator.CreateInstance(typeof(SettingsMgr<>).MakeGenericType(typeof(T)),
        [typeof(T).Name,typeof(AppSettingsService).Namespace,type]) is {} mgr)
            _settingsMgrs.Add(typeof(T),mgr);
        return this;
    }
    public SettingsMgr<T>? GetConfigMgr<T>() where T : class{
        if(_settingsMgrs.TryGetValue(typeof(T),out var mgr))
            return (SettingsMgr<T>)mgr;
        return null;
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
    public async void LoadAsync(Action<bool> callback){
        GlobalConstants.ConfigManager = this;
        foreach (var mgr in _settingsMgrs.Values)
        {
            var loadMethod = mgr.GetType().GetMethod("LoadAsync");
            if (loadMethod?.Invoke(mgr, null) is Task<bool> task)
                await task;
        }
        callback(true);
    }
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
