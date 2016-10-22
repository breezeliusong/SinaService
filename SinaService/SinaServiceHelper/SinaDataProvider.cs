using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Security.Credentials;
using Windows.Storage;

namespace SinaService.SinaServiceHelper
{
    public class SinaDataProvider
    {
        /// <summary>
        /// store data object
        /// </summary>
        private readonly SinaOAuthTokens tokens;

        /// <summary>
        /// Password vault used to store access tokens
        /// 
        /// Represents a Credential Locker of credentials. 
        /// The contents of the locker are specific to the app or service. Apps and services don't have access to credentials associated with other apps or services.
        /// </summary>
        private readonly PasswordVault vault;

        public bool LoggedIn { get; private set; }

        public SinaDataProvider(SinaOAuthTokens tokens)
        {
            this.tokens = tokens;
            vault = new PasswordVault();
        }

        //
        private PasswordCredential PasswordCredential
        {
            get
            {
                // Password vault remains when app is uninstalled so checking the local settings value
                if (ApplicationData.Current.LocalSettings.Values["SinaScreenName"] == null)
                {
                    return null;
                }
                var passwordCredentials = vault.RetrieveAll();
                var temp = passwordCredentials.FirstOrDefault(c => c.Resource == "SinaAccessToken");
                if (temp == null)
                {
                    return null;
                }
                return vault.Retrieve(temp.Resource, temp.UserName);
            }
        }

        /// <summary>
        /// log user in to sina
        /// </summary>
        /// <returns>Boolean indicating login success</returns>
        public async Task<bool> LoginAsync()
        {
            //不直接对PasswordCredential 属性进行操作，而是传给一个局部变量，保证数据安全正确
            var sinaCredentials = PasswordCredential;
            //非第一次获取token，直接将第一次授权获取存储到本地的token传入
            if (sinaCredentials != null)
            {
                tokens.AccessToken = sinaCredentials.UserName;
                //TODO
                return true;
            }
            //TODO 
            //if (!await InitializeRequestAccessTokens(tokens.AppKey, tokens.CallbackUri))
            //{
            //    LoggedIn = false;
            //    return false;
            //}
            string requestToken = tokens.RequestToken;
            //TODO
            var sinaUrl = "https://api.weibo.com/oauth2/authorize";
            sinaUrl += "?" + "client_id=" + tokens.AppKey + "&redirect_uri=" + Uri.EscapeDataString(tokens.CallbackUri);
            Uri sinaUri = new Uri(sinaUrl);
            var result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, sinaUri, new Uri(tokens.CallbackUri));
            Debug.WriteLine(result.ResponseData.ToString());
            //https://api.weibo.com/oauth2/default.html?code=b8173b67d65c0377b2cbb5085a920e8e
            switch (result.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    LoggedIn = true;
                    string responseData = result.ResponseData;
                    string codeData = responseData.Substring(responseData.IndexOf("code"));
                    string[] splits = codeData.Split('=');
                    string code = splits[1];
                    Debug.WriteLine(code);
                    //https://api.weibo.com/oauth2/access_token?client_id=YOUR_CLIENT_ID&client_secret=YOUR_CLIENT_SECRET&grant_type=authorization_code&redirect_uri=YOUR_REGISTERED_REDIRECT_URI&code=CODE
                    //string url ="https://api.weibo.com/oauth2/access_token?client_id="+ tokens.AppKey +"&client_secret="+ tokens.AppSecret +"&grant_type=authorization_code&redirect_uri="+ tokens.CallbackUri +"&code="+code;
                    string url = "https://api.weibo.com/oauth2/access_token";
                    Debug.WriteLine(url);

                    string HeaderParams = "client_id =\"" + tokens.AppKey + "\",client_secret =\"" + tokens.AppSecret + "\", grant_type =\"+authorization_code+\" ,redirect_uri =\"" + tokens.CallbackUri + "\",code = " + code + "\"";

                    using (var client = new HttpClient())
                    {
                        var values = new Dictionary<string, string> 

                         {
                             { "client_id", tokens.AppKey },
                             { "client_secret",tokens.AppSecret},
                            { "grant_type", "authorization_code" },
                             { "redirect_uri",tokens.CallbackUri },
                            { "code",code }
                         };

                        var content = new FormUrlEncodedContent(values);

                        var response = await client.PostAsync(url, content);

                        var responseString = await response.Content.ReadAsStringAsync();
                        //"access_token":"2.004hKTOD0swxup0a1276a3da0VXm1B","remind_in":"157679999","expires_in":157679999,"uid":"2962219841"
                        Debug.WriteLine(responseString);
                    }

                    //HttpClientHandler handler = new HttpClientHandler();
                    //if (handler.SupportsAutomaticDecompression)
                    //{
                    //    handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
                    //}
                    //using (HttpClient httpClient = new HttpClient(handler))
                    //{
                    //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("OAuth", HeaderParams);

                    //    string getResponse = await httpClient.PostAsync(new Uri(url), new StringContent(System.Text.Encoding.UTF8));
                    //        Debug.WriteLine(getResponse);

                    //}


                    //HttpClient client = new HttpClient();
                    //try
                    //{
                    //string accessToken=await client.GetStringAsync(url);
                    //Debug.WriteLine(accessToken);
                    //}
                    //catch(HttpRequestException e)
                    //{
                    //    Debug.WriteLine(e.Message);
                    //}
                    return false;



            }
            return true;
        }

        //提取code

        //private async Task<bool> InitializeRequestAccessTokens(string AppKey, string callbackUrl)
        //{
        //    var sinaUrl = "https://api.weibo.com/oauth2/authorize";
        //    sinaUrl += "?" + "client_id=" + AppKey + "&redirect_uri=" + Uri.EscapeDataString(callbackUrl);
        //    string getResponse;
        //    var handler = new HttpClientHandler();
        //    if (handler.SupportsAutomaticDecompression)
        //    {
        //        handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
        //    }
        //    using (HttpClient httpClient = new HttpClient(handler))
        //    {
        //        try
        //        {
        //            getResponse = await httpClient.GetStringAsync(new Uri(sinaUrl));
        //        }
        //        catch (HttpRequestException e)
        //        {
        //            Debug.WriteLine(e.Message);
        //            return false;
        //        }
        //    }

        //    string[] keyValPairs = getResponse.Split('&');
        //    //TODO
        //    return true;
        //}
    }
}
