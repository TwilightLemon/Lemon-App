using LemonApp.Common;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LemonApp.Services;
internal class AppSettingsService(
    ILogger<AppSettingsService> logger
    ) :IHostedService,IConfigManager
{
    private readonly ILogger<AppSettingsService> _logger=logger;
    private Dictionary<Type, object> _settingsMgrs = [];

    public AppSettingsService AddConfig<T>() where T : class{
        if(Activator.CreateInstance(typeof(SettingsMgr<>).MakeGenericType(typeof(T)),
        [typeof(T).Name,typeof(AppSettingsService).Namespace]) is {} mgr)
            _settingsMgrs.Add(typeof(T),mgr);
        return this;
    }
    public SettingsMgr<T>? GetConfigMgr<T>() where T : class{
        if(_settingsMgrs.TryGetValue(typeof(T),out var mgr))
            return (SettingsMgr<T>)mgr as SettingsMgr<T>;
        return null;
    }
    public async void LoadAsync(Action<bool> callback){
        GlobalConstants.ConfigManager = this;
        foreach (var mgr in _settingsMgrs.Values)
        {
            var loadMethod = mgr.GetType().GetMethod("Load");
            if (loadMethod?.Invoke(mgr, null) is Task<bool> task)
                await task;
        }
        callback(true);
    }
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach(var mgr in _settingsMgrs.Values){
            var saveMethod = mgr.GetType().GetMethod("Save");
            if(saveMethod?.Invoke(mgr,null) is Task<bool> task)
                await task;
        }
    }
}
