using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using LemonApp.Common.WinAPI;
using LemonApp.Native;
using Newtonsoft.Json;

namespace LemonApp.Common.Configs;
public class Appearance{
    #region comment
    [JsonPropertyName("$ColorMode")]
    public string _colormode_comment { get; set; } = "ColorMode: Auto / Dark / Light";
    [JsonPropertyName("$AccentColorMode")]
    public string _accentcolormode_comment { get; set; } = "AccentColorMode: Auto / Custom / Music (change with the cover of current playing music)";
    [JsonPropertyName("$BackgroundMode")]
    public string _backgroundmode_comment { get; set; } = "BackgroundMode: Acrylic / Mica / MicaAlt / Image / Music / Color";
    [JsonPropertyName("$BackgroundPath")]
    public string _backgroundpath_comment { get; set; } = "BackgroundPath: Path to the image file, only used when BackgroundMode is Image";
    #endregion

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
    public enum AccentColorType{Auto,Custom,Music}
    /// <summary>
    /// 主题色模式   
    /// </summary>
    public AccentColorType AccentColorMode { get; set; }
    public Color? AccentColor { get; set; }=null;
    public Color? GetAccentColor() => AccentColorMode switch{
        AccentColorType.Custom => AccentColor,
        AccentColorType.Auto => SystemThemeAPI.GetSystemAccentColor(GetIsDarkMode()),
        AccentColorType.Music=> AccentColor,
        _ => null
    };
    public Color? GetFocusAccentColor() => AccentColorMode switch {
        AccentColorType.Custom=> AccentColor?.ApplyColorMode(GetIsDarkMode()),
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
    public string BackgroundPath { get; set; } = "";

    /// <summary>
    /// 窗口大小
    /// </summary>
    public Size WindowSize { get; set; } = new(0,0);
}