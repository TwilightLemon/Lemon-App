using LemonApp.Common.Configs;

namespace LemonApp.Common;
/*
 Config models: ./Congifs  (types  and functions)
 Value updated by AppSettingsService.cs in Main project
 */
/// <summary>
/// Global constants, items are reflected by AppSettingsService
/// </summary>
public static class GlobalConstants
{
    public static IConfigManager? ConfigManager { get; set; }
}
