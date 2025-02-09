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
            if (LoginTenc)
            {
                wb.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
                wb.CoreWebView2.Navigate("https://graph.qq.com/oauth2.0/show?which=Login&display=pc&response_type=code&client_id=100497308&redirect_uri=https%3A%2F%2Fy.qq.com%2Fportal%2Fwx_redirect.html%3Flogin_type%3D1%26surl%3Dhttps%253A%252F%252Fy.qq.com%252Fn%252Fryqq%252Fprofile%252Flike%252Fsong&state=state&display=pc&scope=get_user_info%2Cget_app_friends");
            }
            else
            {
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

        private async void CoreWebView2_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            string url = wb.CoreWebView2.Source;
            if (url.Contains("y.qq.com/n/ryqq/profile/like/song"))
            {
                var cookie = await wb.CoreWebView2.CookieManager.GetCookiesAsync("https://y.qq.com");
                var cookie2 = await wb.CoreWebView2.CookieManager.GetCookiesAsync("https://graph.qq.com");
                var cookieDict = cookie.ToDictionary(i => i.Name, i => i.Value);

                if (cookie2.FirstOrDefault(i=>i.Name=="p_skey") is { } p_skey&&
                    cookie.FirstOrDefault(i=>i.Name=="uin") is { } uin)
                {
                    string g_tk = TencLogin.CalculateGtk(p_skey.Value);
                    string qq = uin.Value;
                    string cookieStr = ToCookieString(cookieDict);
                    OnLoginTenc?.Invoke(new TencUserAuth()
                    {
                        Cookie=cookieStr,
                        Id=qq,
                        G_tk=g_tk
                    });
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
    }
}
