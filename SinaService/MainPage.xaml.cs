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

//"767169950", "9a712310df02a0d481efd210b3cd44e8", "https://api.weibo.com/oauth2/default.html"
//"2758393393","31337180683ceead6540ed5240244eb4","https://api.weibo.com/oauth2/default.html"
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
            ring.Visibility = Visibility.Visible;
            ring.IsActive = true;
            if (!await Tools.CheckInternetConnection())
            {
                return;
            }
            
            SinaServiceHelper.SinaService.Instance.Initialize(Key.Text, Secret.Text,Callback.Text);

            if (!await SinaServiceHelper.SinaService.Instance.LoginAsync())
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
            ring.Visibility = Visibility.Collapsed;


        }

        private async void GetTimeLine(object sender, RoutedEventArgs e)
        {
            ring.Visibility = Visibility.Visible;
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
            ring.Visibility = Visibility.Collapsed;
            StatusListView.DataContext = status;
        }

        //分享一条微博
        private async void Share(object sender, RoutedEventArgs e)
        {
            ring.Visibility = Visibility.Visible;
            if (!await Tools.CheckInternetConnection())
            {
                await new MessageDialog("Unable to connect to Internet").ShowAsync();
                ring.Visibility = Visibility.Collapsed;
                return;
            }
            bool response = await SinaServiceHelper.SinaService.Instance.ShareStatusAsync(Content.Text);
            if (response)
            {
                await new MessageDialog("you have posted a weibo Successfully").ShowAsync();
                Content.Text = string.Empty;
            }
            else await new MessageDialog("you have posted a weibo failed").ShowAsync();

            ring.Visibility = Visibility.Collapsed;
        }

        //分享一条带图片的微博
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

            if (file != null & Content.Text != null)
            {
                using (IRandomAccessStream stream = await file.OpenReadAsync())
                {
                    BitmapImage image = new BitmapImage();
                    await image.SetSourceAsync(stream);
                    Image1.Source = image;
                }
                if (!await Tools.CheckInternetConnection())
                {
                    await new MessageDialog("Unable to connect to Internet").ShowAsync();
                    return;
                }
                bool response = await SinaServiceHelper.SinaService.Instance.ShareStatusWithPicture(Content.Text, file);
                if (response)
                {
                    await new MessageDialog("you have posted Successfully").ShowAsync();
                    Content.Text = string.Empty;
                    Image1.Source = null;
                    return;
                }
                await new MessageDialog("you have posted a weibo failed").ShowAsync();
            }
            else
            {
                await new MessageDialog("you should say something and add a picture").ShowAsync();
            }

        }
    }
}
