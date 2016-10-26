using SinaService.SinaServiceHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!await Tools.CheckInternetConnection())
            {
                return;
            }

            SinaServiceHelper.SinaService.Instance.Initialize("767169950", "9a712310df02a0d481efd210b3cd44e8", "https://api.weibo.com/oauth2/default.html");
            if(!await SinaService.SinaServiceHelper.SinaService.Instance.LoginAsync())
            {
                var error = new MessageDialog("Unable to log to Sina");
                await error.ShowAsync();
                return;
            }

            //TODO 获取用户信息
            SinaServiceHelper.SinaService.Instance.GetUserAsync();
             

        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string url = "https://api.weibo.com/2/statuses/user_timeline.json?access_token=2.004hKTOD0swxup0a1276a3da0VXm1B";
            HttpClient client = new HttpClient();
            var response=await client.GetAsync(new Uri(url));
           string result= await response.Content.ReadAsStringAsync();
            Debug.WriteLine(result);
        }
    }
}
