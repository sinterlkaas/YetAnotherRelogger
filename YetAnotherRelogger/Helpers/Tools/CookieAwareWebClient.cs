using System;
using System.Net;

namespace YetAnotherRelogger.Helpers.Tools
{
    public class CookieAwareWebClient : WebClient
    {
        private readonly CookieContainer _cookies = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            var webRequest = base.GetWebRequest(address);
            if (webRequest != null && webRequest.GetType() == typeof(HttpWebRequest))
            {
                ((HttpWebRequest)webRequest).CookieContainer = _cookies;
            }
            return webRequest;
        }

        public CookieContainer Cookies
        {
            get
            {
                return _cookies;
            }
        }

        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(CookieAwareWebClient));
            Headers = ((System.Net.WebHeaderCollection)(resources.GetObject("$this.Headers")));
        }
    }
}
