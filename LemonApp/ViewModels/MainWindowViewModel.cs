using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace LemonApp.ViewModels;
public partial class MainWindowViewModel:ObservableObject
{
    public record MainMenu(string Name, Geometry Icon, Type PageType);
    public ObservableCollection<MainMenu> MainMenus { get; set; } = [
        new MainMenu("Home", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), null),
        new MainMenu("PlayList", Geometry.Parse("M0,0 L24,0 24,24 0,24 Z"), null)
        ];

    public bool IsLyricPageOpen { get; set; } = false;
}
