using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LemonApp.Common.Funcs
{
    public class HttpHelper
    {
        public static async Task<string> GetRedirectUrl(string url)
        {
            try
            {
                using var hc = new HttpClient(new SocketsHttpHandler() { AllowAutoRedirect = false });
                var headers = await hc.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                return headers.Headers.Location.ToString();
            }
            catch
            {
                return null;
            }
        }

        public static async Task<long> GetHTTPFileSize(HttpClient hc,string url)
        {
            long size;
            try
            {
                var headers = await hc.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                return headers.StatusCode == HttpStatusCode.OK ? (long)headers.Content.Headers.ContentLength : 0;
            }
            catch
            {
                size = 0L;
            }
            return size;
        }
    }
}
