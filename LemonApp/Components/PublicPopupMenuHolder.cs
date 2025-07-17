using CommunityToolkit.Mvvm.Input;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using LemonApp.ViewModels;
using LemonApp.Views.UserControls;
using System.Collections.Generic;
using System.Windows.Input;

namespace LemonApp.Components;

public class PublicPopupMenuHolder
{
    public static IRelayCommand? GotoArtistCommand,GoToAlbumCommand;
    public static IAsyncRelayCommand<IList<Music>>? AddToMyDissCommand;
    public static IRelayCommand? AddToQuickAccessCommand,RemoveQuickAccessCommand;
    public static IRelayCommand? ShowMusicOptionsCommand;
    public readonly PopupSelector selector;
    public PublicPopupMenuHolder(PopupSelector selector,MainWindowViewModel mainWindowViewModel)
    {
        this.selector = selector;
        AddToMyDissCommand = selector.AddToMyDissCommand;
        GotoArtistCommand = selector.CheckIfGotoArtistsPopupCommand;
        GoToAlbumCommand = selector.GoToAlbumPageCommand;
        ShowMusicOptionsCommand = selector.ShowMusicOptionsPopupCommand;
        AddToQuickAccessCommand = mainWindowViewModel.AddToQuickAccessCommand;
        RemoveQuickAccessCommand = mainWindowViewModel.RemoveQuickAccessCommand;
    }
}
