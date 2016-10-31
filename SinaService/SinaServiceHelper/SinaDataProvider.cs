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
using Windows.Web.Http;

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
        /// Represents a Credential Locker of credentials. 
        /// The contents of the locker are specific to the app or service. Apps and services don't have access to credentials associated with other apps or services.
        /// </summary>
        //private readonly PasswordVault vault;

        public string UserUid { get; set; }

        public bool LoggedIn { get; private set; }

        public SinaDataProvider(SinaOAuthTokens tokens)
        {
            this.tokens = tokens;
        }
        private ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

        /// <summary>
        /// log user in to sina
        /// 判断是否登录
        /// </summary>
        /// <returns>Boolean indicating login success</returns>
        public async Task<bool> LoginAsync()
        {
            //非第一次获取token，直接将第一次授权获取存储到本地的token传入
            if (settings.Values["app_key"]!=null )
            {
                if (settings.Values["app_key"].ToString() == tokens.AppKey&&settings.Values["access_token"] != null)
                {
                    tokens.AccessToken = settings.Values["access_token"].ToString();
                    tokens.uid = ApplicationData.Current.LocalSettings.Values["SinaUid"].ToString();
                    LoggedIn = true;
                    return true;
                }
            }


            var sinaUrl = "https://api.weibo.com/oauth2/authorize";
            sinaUrl += "?" + "client_id=" + tokens.AppKey + "&redirect_uri=" + Uri.EscapeDataString(tokens.CallbackUri) + "&forcelogin=true";
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


            using (var client = new System.Net.Http.HttpClient())
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
                    tokens.uid = uid;

                    ApplicationData.Current.LocalSettings.Values["access_token"] = access_token;
                    ApplicationData.Current.LocalSettings.Values["SinaUid"] = uid;//2962219841
                    settings.Values["app_key"] = tokens.AppKey;
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
            using (var client = new System.Net.Http.HttpClient())
            {
                var values = new Dictionary<string, string>
                         {
                             { "access_token", tokens.AccessToken },
                             { "status",text},
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
            var status = await getMessage<UserStatus>(url);
            return status;
        }

        //获取用户信息
        public async Task<SinaUser> GetUserAsync(string uid = null)
        {
            var UserUid = uid ?? tokens.uid;
            string url = "https://api.weibo.com/2/users/show.json?uid=" + UserUid + "&access_token=" + tokens.AccessToken;
            string result = await HttpRequest.SendGetRequest(url);
            return JsonConvert.DeserializeObject<SinaUser>(result);
        }

        private async Task<T> getMessage<T>(string url)
        {
            string result = await HttpRequest.SendGetRequest(url);
            Debug.Write(result);
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

        public async Task<bool> ShareStatusWithPicture(string text, StorageFile file)
        {
            Windows.Web.Http.HttpClient client = new Windows.Web.Http.HttpClient();
            var fileContent = new HttpStreamContent(await file.OpenAsync(FileAccessMode.Read));
            fileContent.Headers.Add("Content-Type", "multipart/form-data");
            Debug.Write(fileContent.Headers.ContentType.ToString());
            var content = new HttpMultipartFormDataContent();
            Uri uri = new Uri("https://upload.api.weibo.com/2/statuses/upload.json");
            content.Add(fileContent, "pic", file.Name);
            content.Add(new HttpStringContent(tokens.AccessToken), "access_token");
            content.Add(new HttpStringContent(Uri.EscapeDataString(text)), "status");
            Windows.Web.Http.HttpResponseMessage msg = await client.PostAsync(uri, content);
            client.Dispose();
            return msg.IsSuccessStatusCode;
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
