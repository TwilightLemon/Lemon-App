using System;

namespace LemonApp.Services;
public enum PageType
{
    SettingsPage, AlbumPage, PlaylistPage, SearchPage, HomePage, RankPage,ArtistPage, RadioPage,AccountInfoPage,Notification,SongsOfSinger,AlbumsOfSinger,CommentPage
}
public class MainNavigationService
{
    public event Action<PageType,object?>? OnNavigationRequested;
    public event Action? LoadingAniRequested,LoadingAniCancelled;

    public void RequstNavigation<T>(PageType type,T? arg) where T:class
    {
        OnNavigationRequested?.Invoke(type, arg);
    }
    public void RequstNavigation(PageType type)
    {
        OnNavigationRequested?.Invoke(type,null);
    }

    public void BeginLoadingAni()=>App.Current.Dispatcher.Invoke(LoadingAniRequested);
    public void CancelLoadingAni()=>App.Current.Dispatcher.Invoke(LoadingAniCancelled);
}
