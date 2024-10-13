using System.Windows;
using System.Windows.Media;
using LemonApp.Common.WinAPI;

namespace LemonApp.Common.Configs;
public class Appearence{
    public enum ColorModeType{Auto,Dark,Light}
    
    /// <summary>
    /// 全局暗亮色模式
    /// </summary>
    public ColorModeType ColorMode { get; set; }
    public bool GetIsDarkMode() =>  ColorMode switch
    {
        ColorModeType.Dark => true,
        ColorModeType.Light => false,
        ColorModeType.Auto => !SystemThemeAPI.GetIsLightTheme(),
        _ => true //default to dark
    };
    public enum AccentColorType{Auto,Custome}
    public AccentColorType AccentColorMode { get; set; }

    public Color? AccentColor { get; set; }=null;
    public Color? GetAccentColor() => AccentColorMode switch{
        AccentColorType.Custome => AccentColor,
        AccentColorType.Auto => SystemParameters.WindowGlassColor,
        _ => null
    };
}