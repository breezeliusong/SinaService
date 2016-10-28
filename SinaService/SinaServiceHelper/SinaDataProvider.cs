using Newtonsoft.Json;
using SinaService.Model;
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
using Windows.UI.Popups;

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

        public string UserUid { get; set; }

        public bool LoggedIn { get; private set; }

        public SinaDataProvider(SinaOAuthTokens tokens)
        {
            this.tokens = tokens;
            vault = new PasswordVault();
        }

        // 密码存储access_token
        private PasswordCredential PasswordCredential
        {
            get
            {
                // Password vault remains when app is uninstalled so checking the local settings value
                if (ApplicationData.Current.LocalSettings.Values["Sina_expires_in"] == null)
                {
                    return null;
                }

                string time = ApplicationData.Current.LocalSettings.Values["Sina_expires_in"].ToString();
                int expires = Int32.Parse(time);
                if (expires <= 0)
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
        /// 判断是否登录
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
                tokens.uid = sinaCredentials.Password;
                LoggedIn = true;
                return true;
            }


            var sinaUrl = "https://api.weibo.com/oauth2/authorize";
            sinaUrl += "?" + "client_id=" + tokens.AppKey + "&redirect_uri=" + Uri.EscapeDataString(tokens.CallbackUri);
            Uri sinaUri = new Uri(sinaUrl);
            WebAuthenticationResult result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, sinaUri, new Uri(tokens.CallbackUri));

            Debug.WriteLine(result.ResponseData.ToString());
            //https://api.weibo.com/oauth2/default.html?code=b8173b67d65c0377b2cbb5085a920e8e

            switch (result.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    await GetAccessTokenAsync(result);
                    LoggedIn = true;
                    return true;
                case WebAuthenticationStatus.UserCancel:
                    //TODO 对用户取消进行处理
                    LoggedIn = false;
                    return false;
                case WebAuthenticationStatus.ErrorHttp:
                    LoggedIn = false;
                    return false;
            }
            return true;
        }



        /// <summary>
        /// 获取Access_token
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task<bool> GetAccessTokenAsync(WebAuthenticationResult result)
        {
            string responseData = result.ResponseData;
            string codeData = responseData.Substring(responseData.IndexOf("code"));
            string[] splits = codeData.Split('=');
            string code = splits[1];
            Debug.WriteLine(code);
            string url = "https://api.weibo.com/oauth2/access_token";
            Debug.WriteLine(url);


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
                if (response.IsSuccessStatusCode)
                {

                    var responseString = await response.Content.ReadAsStringAsync();
                    string respnseResult = FixInvalidCharset(responseString);
                    string access_token = ExtractMessageFromResponse(respnseResult, "access_token");
                    string expires_in = ExtractMessageFromResponse(respnseResult, "expires_in");
                    string uid = ExtractMessageFromResponse(respnseResult, "uid");

                    tokens.AccessToken = access_token;
                    tokens.expires_in = expires_in;
                    tokens.uid = uid;

                    //将需要的信息存储到本地
                    var passwordCredential = new PasswordCredential("SinaAccessToken", access_token, uid);
                    ApplicationData.Current.LocalSettings.Values["Sina_expires_in"] = expires_in;
                    vault.Add(passwordCredential);
                    return true;
                }
                else
                {
                    await new MessageDialog("there is an exception in post a access_token").ShowAsync();
                    return false;
                }
            }
        }

        //发布一条微博（无图片）
        public async Task<bool> ShareStatusAsync(string text)
        {
            bool result = false;
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                         {
                             { "access_token", tokens.AccessToken },
                             { "status",Uri.EscapeDataString(text)},
                         };
                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync("https://api.weibo.com/2/statuses/update.json", content);
                result = response.IsSuccessStatusCode;
            }
            return result;
        }

        //获取用户状态
        public async Task<UserStatus> GetUserTimeLineAsync()
        {
            string url = "https://api.weibo.com/2/statuses/user_timeline.json?access_token=" + tokens.AccessToken;
            var status= await getMessage<UserStatus>(url);
            return status;
        }

        //获取用户信息
        public async Task<SinaUser> GetUserAsync(string uid=null)
        {
            var UserUid = uid ?? tokens.uid;
            string url = "https://api.weibo.com/2/users/show.json?uid=" + UserUid+ "&access_token="+tokens.AccessToken;
            string result=await HttpRequest.SendGetRequest(url);
            return JsonConvert.DeserializeObject<SinaUser>(result);
        }

        private async Task<T> getMessage<T>(string url)
        {
            string result = await HttpRequest.SendGetRequest(url);
            return JsonConvert.DeserializeObject<T>(result);
        }

        //获取需要的字符串
        private string ExtractMessageFromResponse(string response, string message)
        {
            if (response != null)
            {
                string codeData = response.Substring(response.IndexOf(message));
                string[] splits = codeData.Split(',');

                string[] pairs = splits[0].Split(':');
                return pairs[1];
            }
            return string.Empty;
        }

        //去除无效的符号
        private string FixInvalidCharset(string text)
        {
            // Fix invalid charset returned by some web sites.
            if (text.Contains("\""))
            {
                text = text.Replace("\"", string.Empty);
            }
            if (text.Contains("{"))
            {
                text = text.Replace("{", string.Empty);
            }
            if (text.Contains("}"))
            {
                text = text.Replace("}", string.Empty);
            }
            return text;
        }


        //HttpClient httpClient = new HttpClient();
        //MultipartFormDataContent form = new MultipartFormDataContent();

        //form.Add(new StringContent(username), "username");
        //form.Add(new StringContent(useremail), "email");
        //form.Add(new StringContent(password), "password");
        //form.Add(new StringContent(usertype), "user_type");
        //form.Add(new StringContent(subjects), "subjects");
        //form.Add(new ByteArrayContent(imagebytearraystring, 0, imagebytearraystring.Count()), "profile_pic", "hello1.jpg");
        //HttpResponseMessage response = await httpClient.PostAsync("PostUrl", form);

        //response.EnsureSuccessStatusCode();
        //httpClient.Dispose();
        //string sd = response.Content.ReadAsStringAsync().Result;

    }
}
