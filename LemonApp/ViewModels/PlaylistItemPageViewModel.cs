using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LemonApp.MusicLib.Abstraction.Playlist.DataTypes;

namespace LemonApp.ViewModels;
public partial class PlaylistItemPageViewModel(
    MainNavigationService mainNavigationService) :ObservableObject
{
    private readonly MainNavigationService _mainNavigationService = mainNavigationService;
    [ObservableProperty]
    private string _title = string.Empty;
    [ObservableProperty]
    private Playlist? _choosenItem;

    partial void OnChoosenItemChanged(Playlist? value)
    {
        if (value!=null)
        {
            _mainNavigationService.RequstNavigation(PageType.PlaylistPage, value.Id);
        }
    }

    public ObservableCollection<Playlist> Playlists { get; set; } = [];
}
