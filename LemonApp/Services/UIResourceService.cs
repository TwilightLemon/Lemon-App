using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LemonApp.Common.Behaviors;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.Views.Windows;

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
    private SettingsMgr<Appearence>? _settingsMgr;
    public bool GetIsDarkMode() { 
        if(_settingsMgr== null)
        {
            _settingsMgr = appSettingsService.GetConfigMgr<Appearence>();
            if (_settingsMgr == null) throw new InvalidOperationException("SettingsMgr has not initialized yet.");
            _settingsMgr.OnDataChanged += SettingsMgr_OnDataChanged;
        }
        return _settingsMgr?.Data?.GetIsDarkMode() == true;
    }

    private void SettingsMgr_OnDataChanged()
    {
        App.Current.Dispatcher.Invoke(() => { 
        UpdateColorMode();
        UpdateAccentColor();
        UpdateThemeConfig();
        });
    }

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

    public void UpdateThemeConfig()
    {
        var mgr=appSettingsService.GetConfigMgr<Appearence>();
        if (mgr == null||mgr.Data==null) return;
        var mw = App.Current.MainWindow;
        mw.Container.Background = null;
        switch (mgr.Data.BackgroundMode)
        {
            case Appearence.BackgroundType.Acrylic:
                mw.Mode=Common.WinAPI.MaterialType.Acrylic;
                mw.SetResourceReference(Control.BackgroundProperty, "WindowBackgroundColor");
                break;
            case Appearence.BackgroundType.Mica:
                mw.Mode = Common.WinAPI.MaterialType.Mica;
                mw.Background = Brushes.Transparent;
                break;
            case Appearence.BackgroundType.Image:
                if (System.IO.Path.Exists(mgr.Data.BackgroundPath))
                {
                    mw.Mode = Common.WinAPI.MaterialType.None;
                    mw.Background = new ImageBrush(new BitmapImage(new Uri(mgr.Data.BackgroundPath))) { Stretch = Stretch.UniformToFill };
                    mw.Container.SetResourceReference(Control.BackgroundProperty, "WindowBackgroundColor");
                }
                break;
            case Appearence.BackgroundType.Color:
                mw.Mode = Common.WinAPI.MaterialType.None;
                mw.SetResourceReference(Window.BackgroundProperty, "BackgroundColor");
                break;
        }
    }
}