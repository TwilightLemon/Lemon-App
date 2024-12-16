using CommunityToolkit.Mvvm.ComponentModel;
using LemonApp.Common.Configs;
using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.Entities;
using LemonApp.Services;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace LemonApp.ViewModels
{
    public partial class SettingsPageViewModel:ObservableObject
    {
        public SettingsPageViewModel(IHttpClientFactory httpClientFactory,AppSettingsService appSettingsService)
        {
            _playingMgr = appSettingsService.GetConfigMgr<PlayingPreference>()!;
            _httpClient = httpClientFactory.CreateClient(App.PublicClientFlag);
            _assemblyName = _assembly.GetName().Name!;
            _assemblyVersion = _assembly.GetName().Version!.ToString();
        }
        readonly HttpClient _httpClient;
        readonly SettingsMgr<PlayingPreference> _playingMgr;
        public void LoadData()
        {
            PreferQuality = _playingMgr.Data.Quality;
        }
        #region Files
        [ObservableProperty]
        private MusicQuality _preferQuality= MusicQuality.SQ;
        partial void OnPreferQualityChanged(MusicQuality value)
        {
            _playingMgr.Data.Quality = value;
        }
        #endregion
        #region About
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        [ObservableProperty]
        private string _assemblyName;
        [ObservableProperty]
        private string _assemblyVersion;
        [ObservableProperty]
        private string _publisherMsg = "Published by twlm.";
        public UIElement? PublisherContent = null;
        public async Task LoadPublisherContent()
        {
            if(PublisherContent == null)
            {
                string xaml = await _httpClient.GetStringAsync("https://gitee.com/TwilightLemon/LemonAppDynamics/raw/master/New_AboutPage.En-US.xaml");
                PublisherContent = (UIElement)XamlReader.Parse(xaml);
            }
        }
        #endregion
    }
}
