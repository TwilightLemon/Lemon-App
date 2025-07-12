using System;

namespace LemonApp.Services;
public enum PageType
{
    /// <summary>
    /// none parameter
    /// </summary>
    SettingsPage,
    /// <summary>
    /// para: string id
    /// </summary>
    AlbumPage,
    /// <summary>
    /// para: Playlist entity
    /// </summary>
    PlaylistPage, 
    /// <summary>
    /// para: string keyword
    /// </summary>
    SearchPage, 
    HomePage,
    RankPage,
    /// <summary>
    /// para: string id or Profile entity
    /// </summary>
    ArtistPage, 
    RadioPage,
    AccountInfoPage,
    /// <summary>
    /// para: string content
    /// </summary>
    Notification,
    /// <summary>
    /// para: string id of singer
    /// </summary>
    SongsOfSinger,
    /// <summary>
    /// para: string id of singer
    /// </summary>
    AlbumsOfSinger,
    /// <summary>
    /// para: music entity
    /// </summary>
    CommentPage
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

    public class LoadingContext:IDisposable
    {
        public LoadingContext(MainNavigationService nav)
        {
            _nav = nav;
            App.Current.Dispatcher.Invoke(nav.LoadingAniRequested);
        }
        private readonly MainNavigationService _nav;
        public void Dispose() => App.Current.Dispatcher.Invoke(_nav.LoadingAniCancelled);
    }
    public LoadingContext BeginLoading() => new(this);

}