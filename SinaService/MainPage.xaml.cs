using Newtonsoft.Json.Linq;
using SinaService.Model;
using SinaService.SinaServiceHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

//“空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409 上有介绍

namespace SinaService
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void LoginIn(object sender, RoutedEventArgs e)
        {
            if (!await Tools.CheckInternetConnection())
            {
                return;
            }

            SinaServiceHelper.SinaService.Instance.Initialize("767169950", "9a712310df02a0d481efd210b3cd44e8", "https://api.weibo.com/oauth2/default.html");
            if (!await SinaService.SinaServiceHelper.SinaService.Instance.LoginAsync())
            {
                var error = new MessageDialog("Unable to log to Sina");
                await error.ShowAsync();
                return;
            }

            //TODO 获取用户信息
            var user = await SinaServiceHelper.SinaService.Instance.GetUserAsync();
            if (user == null)
            {
                await new MessageDialog("Unable to get the user message").ShowAsync();
                return;
            }
            ProfileImage.DataContext = user;
            Description.DataContext = user;


        }

        private async void GetTimeLine(object sender, RoutedEventArgs e)
        {
            var UserStatus = await SinaServiceHelper.SinaService.Instance.GetUserTimeLineAsync();
            if (UserStatus == null)
            {
                await new MessageDialog("Unable to get the user status").ShowAsync();
                return;
            }
            List<Status> statusList = UserStatus.statuses;
            ObservableCollection<Status> status = new ObservableCollection<Status>();
            status.Clear();
            foreach (Status st in statusList)
            {
                status.Add(st);
            }
            this.DataContext = status;
        }

        //分享一条微博
        private async void Share(object sender, RoutedEventArgs e)
        {
            if (!await Tools.CheckInternetConnection())
            {
                await new MessageDialog("Unable to connect to Internet").ShowAsync();
                return;
            }
            bool response = await SinaServiceHelper.SinaService.Instance.ShareStatusAsync(Content.Text);
            if (response) { await new MessageDialog("you have posted a weibo Successfully").ShowAsync(); }
        }

        private async void Share_with_picture(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.FileTypeFilter.Clear();
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".gif");
            openPicker.FileTypeFilter.Add(".png");
            var file = await openPicker.PickSingleFileAsync();

            var values = new Dictionary<string, string>
                         {
                             { "access_token","2.004hKTOD0swxup0a1276a3da0VXm1B"},
                             { "status",Content.Text}
                         };

            SendPostMethod("https://upload.api.weibo.com/2/statuses/upload.json", file, values,  async(con) =>
            {
                var fileContent = new HttpStreamContent(await file.OpenAsync(FileAccessMode.Read));
                fileContent.Headers.Add("Content-Type", "multipart/form-data");
                con.Add(fileContent, "pic", file.Name);
            }, (result) =>
            {

                Debug.Write(result);
            });
            //HttpClient client = new HttpClient();
            ////var headers = client.DefaultRequestHeaders;
            ////headers.Add("access_token", "2.004hKTOD0swxup0a1276a3da0VXm1B");

            //var fileContent = new HttpStreamContent(await file.OpenAsync(FileAccessMode.Read));
            //fileContent.Headers.Add("Content-Type", "multipart/form-data");
            //Debug.Write(fileContent.Headers.ContentType.ToString());
            //var content = new HttpMultipartFormDataContent();
            //Uri uri = new Uri("https://upload.api.weibo.com/2/statuses/upload.json");
            //content.Add(fileContent, "pic",file.Name);
            //content.Add(new HttpStringContent("2.004hKTOD0swxup0a1276a3da0VXm1B"), "access_token");
            //content.Add(new HttpStringContent(Uri.EscapeDataString(Content.Text)), "status");


            //HttpResponseMessage msg = await client.PostAsync(uri, content);
        }

        public async void SendPostMethod(string url, StorageFile file, Dictionary<string, string> param,  Action<HttpMultipartFormDataContent> FileContent, Action<string> response)
        {
            HttpClient client = new HttpClient();
            var headers = client.DefaultRequestHeaders;
            string header = "";
            if (!headers.UserAgent.TryParseAdd(header))
            {
               // throw new Exception("Invalid header value: " + header);
            }
            header = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
            if (!headers.UserAgent.TryParseAdd(header))
            {
              //  throw new Exception("Invalid header value: " + header);
            }
            HttpResponseMessage httpResponse = new HttpResponseMessage();
            Uri requestUri = new Uri(url);
            var content = new HttpMultipartFormDataContent();

            foreach (var item in param)
            {
                content.Add(new HttpStringContent(item.Value), item.Key);

            }
            //var fileContent = new HttpStreamContent(await file.OpenAsync(FileAccessMode.Read));
            //fileContent.Headers.Add("Content-Type", "multipart/form-data");
            //传递出去
               await FileContent.EndInvoke();
            try
            {
                httpResponse = await client.PostAsync(requestUri, content);
                response(await httpResponse.Content.ReadAsStringAsync());

            }
            catch (Exception ex)
            {
                Debug.Write(ex.Message);
            }
        }

        //  FileStream fs = new FileStream(file.Name, FileMode.Open, FileAccess.Read);

        // Create a byte array of file stream length
        //    byte[] ImageData = new byte[fs.Length];

        //Read block of bytes from stream into the byte array
        //   fs.Read(ImageData, 0, System.Convert.ToInt32(fs.Length));

        //Close the File Stream
        //fs.Close();

        // byte[] bytes = System.IO.File.ReadAllBytes(ImageData.ToString());
        //System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
        //MultipartFormDataContent form = new MultipartFormDataContent();

        //form.Add(new StringContent(Uri.EscapeDataString(Content.Text)), "username");
        //form.Add(new ByteArrayContent(bytes, 0, bytes.Count()), "pic", file.Name);
        //System.Net.Http.HttpResponseMessage response = await httpClient.PostAsync(uri, form);

        //response.EnsureSuccessStatusCode();
        //fs.Dispose();
        //httpClient.Dispose();
        //string sd = response.Content.ReadAsStringAsync().Result;

        // string text = await Windows.Storage.FileIO.ReadTextAsync(sampleFile);

        //"2.004hKTOD0swxup0a1276a3da0VXm1B"
        //    HttpRequestMessage mSent = new HttpRequestMessage(HttpMethod.Post, uri);
        //IBuffer temp = await FileIO.ReadBufferAsync(file);
        //var values = new Dictionary<string, object>
        //             {
        //                
        //                 { "status",Uri.EscapeDataString(Content.Text)},
        //             };

        //    mSent.Content = new HttpStringContent(values.ToString(), UnicodeEncoding.Utf8, "application/json; charset=utf-8");
        //    HttpClient client = new HttpClient();
        //    HttpResponseMessage response = await client.SendRequestAsync(mSent);
        //    response.EnsureSuccessStatusCode();
        //using (var client = new System.Net.Http.HttpClient())
        //{


        //    IBuffer temp = await FileIO.ReadBufferAsync(file);
        //    var values = new Dictionary<string, object>
        //             {
        //                 { "pic",temp},
        //                 { "status",Uri.EscapeDataString(Content.Text)},
        //             };
        //    // var content = new FormUrlEncodedContent(values);

        //     var response = await client.PostAsync("https://api.weibo.com/2/statuses/update.json", content);
        //   var  result = response.IsSuccessStatusCode;
        //}


        //if (file != null)
        //{
        //    IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
        //    var fileBytes = new byte[fileStream.Size];
        //    await fileStream.ReadAsync(fileBytes.AsBuffer(), (uint)fileStream.Size, InputStreamOptions.None);
        //    fileStream.Seek(0);

        //    var content = new HttpMultipartFormDataContent();

        //    using (var client = new Windows.Web.Http.HttpClient())
        //    {
        //        using (var content1 =
        //            new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
        //        {
        //            content.Add(new StreamContent(new MemoryStream(fileBytes)), "pic", "upload.jpg");

        //            using (
        //               var message =
        //                   await client.PostAsync("http://www.directupload.net/index.php?mode=upload", content))
        //            {
        //                var input = await message.Content.ReadAsStringAsync();

        //                return !string.IsNullOrWhiteSpace(input) ? Regex.Match(input, @"http://\w*\.directupload\.net/images/\d*/\w*\.[a-z]{3}").Value : null;
        //            }
        //        }
        //    }



        /*
         *var client = new HttpClient();
    var content = new MultipartFormDataContent();
    content.Add(new StreamContent(File.Open("../../Image1.png", FileMode.Open)), "Image", "Image.png");
    var result = client.PostAsync("https://hostname/api/Account/UploadAvatar", content);
    Console.WriteLine(result.Result.ToString());
}

         * 
         */
        //return mediaId.ToString();

        //HttpClient client = new HttpClient();
        //HttpStreamContent streamContent = new HttpStreamContent(stream.AsInputStream());
        //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
        //request.Content = streamContent;
        //HttpResponseMessage response = await client.PostAsync(uri, streamContent);


        //}
        //    public static async Task<string> Upload(byte[] image)
        //{
        //    using (var client = new HttpClient())
        //    {
        //        using (var content =
        //            new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
        //        {
        //            content.Add(new StreamContent(new MemoryStream(image)), "bilddatei", "upload.jpg");

        //            using (
        //               var message =
        //                   await client.PostAsync("http://www.directupload.net/index.php?mode=upload", content))
        //            {
        //                var input = await message.Content.ReadAsStringAsync();

        //                return !string.IsNullOrWhiteSpace(input) ? Regex.Match(input, @"http://\w*\.directupload\.net/images/\d*/\w*\.[a-z]{3}").Value : null;
        //            }
        //        }
        //    }
        //}
    }
}
