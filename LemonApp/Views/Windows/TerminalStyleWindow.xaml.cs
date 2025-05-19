using LemonApp.Common.UIBases;
using LemonApp.Services;
using LemonApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// TerminalStyleWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TerminalStyleWindow : FluentWindowBase
    {
        public TerminalStyleWindow(MainWindowViewModel mv)
        {
            InitializeComponent();
            DataContext = mv;
            visualizer.Player = App.Services.GetRequiredService<MediaPlayerService>().Player;
        }
    }
}
