using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LemonApp.Common.Behaviors;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;

namespace LemonApp.Services;
/*
 UpdateColorMode: system msg -> ApplicationService -> UIResourceService -> other components
 */
/// <summary>
/// 设置全局UI资源
/// </summary>
public class UIResourceService(
    AppSettingService appSettingsService)
{
    public event Action? OnColorModeChanged;
    private SettingsMgr<Appearance>? _settingsMgr;
    public SettingsMgr<Appearance> SettingsMgr
    {
        get
        {
            if (_settingsMgr == null)
            {
                _settingsMgr = appSettingsService.GetConfigMgr<Appearance>();
                if (_settingsMgr == null) throw new InvalidOperationException("SettingsMgr has not initialized yet.");
                _settingsMgr.OnDataChanged += SettingsMgr_OnDataChanged;
            }
            return _settingsMgr;
        }
    }
    public bool GetIsDarkMode() { 
        return SettingsMgr.Data?.GetIsDarkMode() == true;
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
        var dt = SettingsMgr.Data;
        var accentColor=dt?.GetAccentColor();
        var focusColor= dt?.GetFocusAccentColor();
        if (accentColor.HasValue&&focusColor.HasValue){
            App.Current.Resources["HighlightThemeColor"]=new SolidColorBrush(accentColor.Value);
            App.Current.Resources["AccentColorKey"] = accentColor.Value;
            App.Current.Resources["FocusAccentColor"]=new SolidColorBrush(focusColor.Value);
        }
    }

    public bool UsingMusicTheme => SettingsMgr.Data.AccentColorMode == Appearance.AccentColorType.Music;

    public void UpdateThemeConfig()
    {
        var mgr = SettingsMgr;
        if (mgr == null||mgr.Data==null) return;
        var mw = App.Current.MainWindow;
        mw.Container.Background = null;
        switch (mgr.Data.BackgroundMode)
        {
            case Appearance.BackgroundType.Acrylic:
                mw.Mode=Common.WinAPI.MaterialType.Acrylic;
                mw.SetResourceReference(Control.BackgroundProperty, "WindowBackgroundColor");
                break;
            case Appearance.BackgroundType.Mica:
                mw.Mode = Common.WinAPI.MaterialType.Mica;
                mw.Background = Brushes.Transparent;
                break;
            case Appearance.BackgroundType.MicaAlt:
                mw.Mode = Common.WinAPI.MaterialType.MicaAlt;
                mw.Background = Brushes.Transparent;
                break;
            case Appearance.BackgroundType.Image:
                if (System.IO.Path.Exists(mgr.Data.BackgroundPath))
                {
                    mw.Mode = Common.WinAPI.MaterialType.None;
                    mw.Background = new ImageBrush(new BitmapImage(new Uri(mgr.Data.BackgroundPath))) { Stretch = Stretch.UniformToFill };
                    mw.Container.SetResourceReference(Control.BackgroundProperty, "WindowBackgroundColor");
                    mw.MusicControlBgPresenter.UpdateBackground();
                    mw.MusicControlBgPresenter.Visibility = Visibility.Visible;
                }
                break;
            case Appearance.BackgroundType.Color:
                mw.Mode = Common.WinAPI.MaterialType.None;
                mw.SetResourceReference(Window.BackgroundProperty, "BackgroundColor");
                break;
            case Appearance.BackgroundType.Music:
                mw.Mode = Common.WinAPI.MaterialType.None;
                var brush = new ImageBrush();
                BindingOperations.SetBinding(brush, ImageBrush.ImageSourceProperty, new Binding("LyricPageBackgroundSource"));
                mw.Background = brush;
                break;
        }
        if(mgr.Data.BackgroundMode != Appearance.BackgroundType.Image)
        {
            mw.MusicControlBgPresenter.Visibility = Visibility.Collapsed;
        }
    }
}