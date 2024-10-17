using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.User;
using System;
using System.Net.Http;
using System.Windows;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly TencLogin loginAPI;
        private readonly System.Windows.Forms.WebBrowser wb;
        public Action<TencUserAuth>? OnLogin;
        public LoginWindow(IHttpClientFactory clientFactory)
        {
            InitializeComponent();
            wb = new()
            {
                ScriptErrorsSuppressed = true,
                IsWebBrowserContextMenuEnabled = false,
                WebBrowserShortcutsEnabled = false
            };
            wb.DocumentTitleChanged += Wb_DocumentTitleChanged;
            host.Child = wb;
            loginAPI = new TencLogin(clientFactory.CreateClient(App.PublicClientFlag));
            loginAPI.OnAuthCompleted += LoginAPI_OnAuthCompleted;
            Loaded += LoginWindow_Loaded;
        }

        private void LoginAPI_OnAuthCompleted(TencUserAuth obj)
        {
            OnLogin?.Invoke(obj);
            Dispatcher.Invoke(Close);
        }

        private void Wb_DocumentTitleChanged(object? sender, EventArgs e)
        {
            if(wb.Url?.ToString() is { } url &&wb.Document?.Cookie is { } cookie)
                loginAPI.CollectInfo(url, cookie);
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            wb.Navigate(new Uri("https://graph.qq.com/oauth2.0/show?which=Login&display=pc&response_type=code&client_id=100497308&redirect_uri=https%3A%2F%2Fy.qq.com%2Fportal%2Fwx_redirect.html%3Flogin_type%3D1%26surl%3Dhttps%3A%2F%2Fy.qq.com%2Fn%2Fryqq%2Fprofile&state=state&display=pc&scope=get_user_info%2Cget_app_friends"));
        }

    }
}
