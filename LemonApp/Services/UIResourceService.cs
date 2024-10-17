using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LemonApp.Common.Configs;

namespace LemonApp.Services;

/// <summary>
/// 设置全局UI资源
/// </summary>
public class UIResourceService(
    AppSettingsService appSettingsService)
{
    public void UpdateColorMode()
    {
        bool IsDarkMode= appSettingsService.GetConfigMgr<Appearence>()?.Data?.GetIsDarkMode()==true;
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
    }

    public void UpdateAccentColor(){
        var accentColor=appSettingsService.GetConfigMgr<Appearence>()?.Data?.GetAccentColor();
        if(accentColor.HasValue){
            App.Current.Resources["HighlightThemeColor"]=new SolidColorBrush(accentColor.Value);
        }
    }


}