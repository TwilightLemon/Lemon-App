using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;

namespace LemonApp.Common;
/*
 Config models: ./Congifs  (types only)
 Value updated by 
 */
 /// <summary>
 /// Global constants, items are reflected by AppSettingsService
 /// </summary>
public static class GlobalConstants
{
    public static IConfigManager? ConfigManager { get; set; }
}
