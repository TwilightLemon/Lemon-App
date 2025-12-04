using LemonApp.Common.Funcs;
using LemonApp.MusicLib.Abstraction.UserAuth;
using LemonApp.MusicLib.User;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;

namespace LemonApp.Views.Windows
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        public Action<TencUserAuth>? OnLoginTenc;
        public Action<NeteaseUserAuth>? OnLoginNetease;
        public bool LoginTenc { get; set; }=true;
        public LoginWindow(IHttpClientFactory clientFactory)
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }
        private string qurl = "https://graph.qq.com/oauth2.0/show?which=Login&display=pc&response_type=code&client_id=100497308&redirect_uri=https%3A%2F%2Fy.qq.com%2Fportal%2Fwx_redirect.html%3Flogin_type%3D1%26surl%3Dhttps%253A%252F%252Fy.qq.com%252Fn%252Fryqq%252Fprofile%252Flike%252Fsong&state=state&display=pc&scope=get_user_info%2Cget_app_friends";
        private string wurl = "https://open.weixin.qq.com/connect/qrconnect?appid=wx48db31d50e334801&redirect_uri=https%3A%2F%2Fy.qq.com%2Fportal%2Fwx_redirect.html%3Flogin_type%3D2%26surl%3Dhttps%253A%252F%252Fy.qq.com%252Fn%252Fryqq%252Fprofile%252Flike%252Fsong&response_type=code&scope=snsapi_login&state=STATE&href=https%3A%2F%2Fy.qq.com%2Fmediastyle%2Fmusic_v17%2Fsrc%2Fcss%2Fpopup_wechat.css%23wechat_redirect";
        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string? core = null;
            try
            {
                core = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch { }
            if (string.IsNullOrEmpty(core))
            {
                if (ExMessageBox.Show("没有检测到WebView2 Runtime 现在安装吗？"))
                {
                    Process.Start("explorer", "https://developer.microsoft.com/zh-cn/microsoft-edge/webview2/");
                }
                Close();
                return;
            }
            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, CacheManager.GetCachePath(CacheManager.CacheType.Other));
            await wb.EnsureCoreWebView2Async(webView2Environment);
            wb.CoreWebView2.CookieManager.DeleteAllCookies();

            if (LoginTenc)
            {
                wb.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
                wb.CoreWebView2.Navigate(qurl);
                LoginMethodTB.Visibility = Visibility.Visible;
                wb.Margin = new Thickness(0,50,0,0);
            }
            else
            {
                //login with netease
                wb.CoreWebView2.FrameNavigationCompleted += CoreWebView2_FrameNavigationCompleted;
                wb.CoreWebView2.Navigate("https://music.163.com/#/my/");
            }
        }

        private async void CoreWebView2_FrameNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string html = await wb.CoreWebView2.ExecuteScriptAsync("document.body.innerHTML");
            var regex = Regex.Match(html, @"(?<=/user/home\?id=)\d+");
            if (regex.Success)
            {
                var cookie = await wb.CoreWebView2.CookieManager.GetCookiesAsync("https://music.163.com");
                var cookieDict = cookie.ToDictionary(i => i.Name, i => i.Value);
                string id = regex.Value;
                OnLoginNetease?.Invoke(new()
                {
                    Cookie=ToCookieString(cookieDict),
                    Id=id
                });
                Close();
            }
        }

        private string? pskey = null;
        private async void CoreWebView2_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            string url = wb.CoreWebView2.Source;
            if (url.Contains("portal/wx_redirect.html"))
            {
                pskey = TextHelper.FindTextByAB(url, "&code=", "&", 0);
            }
            if (url.Contains("/profile/like/song"))
            {
                var cookie = await wb.CoreWebView2.CookieManager.GetCookiesAsync("https://y.qq.com");
                Dictionary<string, string> cookieDict = [];
                foreach(var c in cookie)
                {
                    cookieDict[c.Name] = c.Value;
                }

                if (cookie.FirstOrDefault(i=>i.Name=="uin"||i.Name=="wxuin") is { } uin&&pskey!=null)
                {
                    string g_tk = TencLogin.CalculateGtk(pskey);
                    string qq = uin.Value;
                    string cookieStr = ToCookieString(cookieDict);
                    OnLoginTenc?.Invoke(new TencUserAuth()
                    {
                        Cookie=cookieStr,
                        Id=qq,
                        G_tk=g_tk
                    });
                    Close();
                }
            }
        }

        private static string ToCookieString(Dictionary<string,string> cookie)
        {
            StringBuilder cookies = new();
            //format cookies string
            foreach (var item in cookie)
            {
                cookies.Append(item.Key);
                cookies.Append('=');
                cookies.Append(item.Value);
                cookies.Append("; ");
            }
            //remove last "; "
            cookies.Remove(cookies.Length - 2, 2);
            return cookies.ToString();
        }

        private void LoginMethodTB_Click(object sender, RoutedEventArgs e)
        {
            pskey = null;
            wb.CoreWebView2.Navigate(LoginMethodTB.IsChecked is true?qurl:wurl);
        }
    }
}
