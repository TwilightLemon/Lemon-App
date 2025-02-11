using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.MusicLib.Playlist;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.Services;

/// <summary>
/// operations for user data, such as Playlist \ Favorite Management
/// </summary>
public class UserDataManager(IHttpClientFactory httpClientFactory
    , UserProfileService auth, MainNavigationService navigation)
{
    readonly HttpClient hc = httpClientFactory.CreateClient(App.PublicClientFlag);
    public async Task<bool> DeleteSongsFromDirid(string dirid, IList<Music> musics, string displayName)
    {
        var success = await PlaylistManageAPI.WriteMusicsToMyDissAsync(hc, auth.GetAuth(), dirid, musics, delete: true);
        navigation.RequstNavigation(PageType.Notification, $"{(success ? "Successfully deleted" : "Failed to delete")} {musics.Count} songs from {displayName}");
        return success;
    }
    public async Task<bool> AddSongsToDirid(string dirid, IList<Music> musics, string displayName)
    {
        var reversed = musics.Reverse().ToList();
        var success = await PlaylistManageAPI.WriteMusicsToMyDissAsync(hc, auth.GetAuth(), dirid, reversed);
        navigation.RequstNavigation(PageType.Notification, $"{(success ? "Successfully added" : "Failed to add")} {reversed.Count} songs to {displayName}");
        return success;
    }
    public async Task<List<Playlist>> GetUserPlaylists()
        => await PlaylistManageAPI.GetMyDissListAsync(hc, auth.GetAuth());
}
