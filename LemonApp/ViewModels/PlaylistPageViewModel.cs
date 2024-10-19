using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace LemonApp.ViewModels;

public partial class PlaylistPageViewModel:ObservableObject
{
    [ObservableProperty]
    private string _listName= "";
    [ObservableProperty]
    private string _description = "";
    [ObservableProperty]
    private Brush? _cover = null;
    [ObservableProperty]
    private string _creatorName = "";
    [ObservableProperty]
    private Brush? _creatorAvatar = null;

    [ObservableProperty]
    private bool _showInfoView = true;

}
