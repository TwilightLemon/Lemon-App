using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using LemonApp.Common.WinAPI;
using LemonApp.Native;

namespace LemonApp.Common.Configs;
public class Appearance{
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
    public enum AccentColorType{Auto,Custome,Music}
    /// <summary>
    /// 主题色模式   
    /// </summary>
    public AccentColorType AccentColorMode { get; set; }
    public Color? AccentColor { get; set; }=null;
    public Color? GetAccentColor() => AccentColorMode switch{
        AccentColorType.Custome => AccentColor,
        AccentColorType.Auto => SystemThemeAPI.GetSystemAccentColor(GetIsDarkMode()),
        AccentColorType.Music=> AccentColor,
        _ => null
    };
    public Color? GetFocusAccentColor() => AccentColorMode switch {
        AccentColorType.Custome=> AccentColor?.ApplyColorMode(GetIsDarkMode()),
        AccentColorType.Auto => SystemThemeAPI.GetSystemAccentColor(GetIsDarkMode(),true),
        AccentColorType.Music=> AccentColor?.ApplyColorMode(GetIsDarkMode()),
        _ =>null
    };
    public enum BackgroundType
    {
        Acrylic,Mica,MicaAlt,Image,Music,Color
    }
    /// <summary>
    /// 背景模式
    /// </summary>
    public BackgroundType BackgroundMode { get; set; }=BackgroundType.Acrylic;
    /// <summary>
    /// 图片背景路径
    /// </summary>
    public string? BackgroundPath { get; set; }

    /// <summary>
    /// 窗口大小
    /// </summary>
    public Size WindowSize { get; set; } = new(0,0);
}