using LemonApp.Common.WinAPI;

namespace LemonApp.Common.Configs;
public class Appearence{
    public enum ColorModeType{Auto,Dark,Light}
    
    public ColorModeType ColorMode { get; set; }
    public bool GetIsDarkMode() =>  ColorMode switch
    {
        ColorModeType.Dark => true,
        ColorModeType.Light => false,
        ColorModeType.Auto => !SystemThemeAPI.GetIsLightTheme(),
        _ => true //default to dark
    };
}