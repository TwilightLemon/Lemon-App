using LemonApp.Common.Funcs;

namespace LemonApp.Common.Configs;
public interface IConfigManager{
    public SettingsMgr<T>? GetConfigMgr<T>() where T : class;
}