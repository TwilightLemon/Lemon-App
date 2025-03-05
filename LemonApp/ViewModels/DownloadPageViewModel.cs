using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LemonApp.Services;
using System.Collections.ObjectModel;

namespace LemonApp.ViewModels;

public partial class DownloadPageViewModel(DownloadService downloadService):ObservableObject
{
    public ObservableCollection<DownloadItemTask> History=> downloadService.History;
    [ObservableProperty]
    private bool _isDownloading = downloadService.IsDownloading;

    [RelayCommand]
    private void Pause()
    {
        downloadService.PauseCurrentTask();
        IsDownloading = downloadService.IsDownloading;
    }

    [RelayCommand]
    private void Resume()
    {
        downloadService.ResumeCurrentTask();
        IsDownloading = downloadService.IsDownloading;
    }

    [RelayCommand]
    private void CancelAll()
    {
        downloadService.CancelAll();
        IsDownloading = false;
    }

    [RelayCommand]
    private void OpenDownloadDir()
    {
        string dir = downloadService.DownloadPath;
        if (System.IO.Directory.Exists(dir))
            System.Diagnostics.Process.Start("explorer.exe", dir);
    }
}
