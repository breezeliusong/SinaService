using SinaService.SinaServiceHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace SinaService.SinaServiceHelper
{
    public static class Tools
    {

        public static async Task<bool> CheckInternetConnection()
        {
            //网络不可用，弹出对话框进行提示
            if (!ConnectionHelper.IsInternetAvailable)
            {
                var dialog = new MessageDialog("Internet connection not detected,Please try again later.");
                await dialog.ShowAsync();
                return false;
            }
            return true;
        }
    }
}
