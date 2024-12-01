using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace LemonApp.ViewModels
{
    public partial class SettingsPageViewModel:ObservableObject
    {
        public SettingsPageViewModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(App.PublicClientFlag);
            _assemblyName = _assembly.GetName().Name!;
            _assemblyVersion = _assembly.GetName().Version!.ToString();
        }
        readonly HttpClient _httpClient;
        #region About
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        [ObservableProperty]
        private string _assemblyName;
        [ObservableProperty]
        private string _assemblyVersion;
        [ObservableProperty]
        private string _publisherMsg = "Published by twlm.";
        #endregion
        public UIElement? PublisherContent = null;
        public async Task LoadPublisherContent()
        {
            if(PublisherContent == null)
            {
                string xaml = await _httpClient.GetStringAsync("https://gitee.com/TwilightLemon/LemonAppDynamics/raw/master/New_AboutPage.En-US.xaml");
                PublisherContent = (UIElement)XamlReader.Parse(xaml);
            }
        }

    }
}
