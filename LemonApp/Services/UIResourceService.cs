using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LemonApp.Common.Behaviors;
using LemonApp.Common.Configs;

namespace LemonApp.Services;
/*
 UpdateColorMode: system msg -> main window -> UIResourceService -> other components
TODO: add weak reference events mgr for mode changed
 */
/// <summary>
/// 设置全局UI资源
/// </summary>
public class UIResourceService(
    AppSettingsService appSettingsService)
{
    public event Action? OnColorModeChanged;
    public bool GetIsDarkMode() =>appSettingsService.GetConfigMgr<Appearence>()?.Data?.GetIsDarkMode()==true;
    public void UpdateColorMode()
    {
        bool IsDarkMode = GetIsDarkMode();
        BlurWindowBehavior.SetDarkMode(IsDarkMode);

        string uri = $"pack://application:,,,/LemonApp.Common;component/Styles/ThemeColor_{IsDarkMode switch
        {
            true => "Dark",
            false => "Light",
        }}.xaml";
        // 移除当前主题资源字典（如果存在）
        var oldDict=App.Current.Resources.MergedDictionaries.FirstOrDefault(d=>d.Source!=null&&d.Source.OriginalString.Contains("Styles/ThemeColor"));
        if(oldDict!=null)
            App.Current.Resources.MergedDictionaries.Remove(oldDict);
        // 添加新的主题资源字典
        App.Current.Resources.MergedDictionaries.Add(new ResourceDictionary(){Source=new Uri(uri,UriKind.Absolute)});

        OnColorModeChanged?.Invoke();
    }

    public void UpdateAccentColor(){
        var dt = appSettingsService.GetConfigMgr<Appearence>()?.Data;
        var accentColor=dt?.GetAccentColor();
        var focusColor= dt?.GetFocusAccentColor();
        if (accentColor.HasValue&&focusColor.HasValue){
            App.Current.Resources["HighlightThemeColor"]=new SolidColorBrush(accentColor.Value);
            App.Current.Resources["FocusAccentColor"]=new SolidColorBrush(focusColor.Value);
        }
    }
}