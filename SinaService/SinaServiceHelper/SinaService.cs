using SinaService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;

namespace SinaService.SinaServiceHelper
{
    public class SinaService
    {
        /// <summary>
        /// private singleton filed for SinaDataProvider.
        /// </summary>
        private static SinaDataProvider sinaDataProvider;

        public SinaDataProvider Provider
        {
            get
            {
                //guarantee tokens!=null
                if (!isInitialized)
                {
                    throw new InvalidOperationException("Provider not initialized.");
                }
                return sinaDataProvider ?? (sinaDataProvider = new SinaDataProvider(tokens));
            }
        }

        /// <summary>
        /// Initializes a new instance
        /// Default private constructor.
        /// </summary>
        private SinaService()
        {
        }

        /// <summary>
        /// private singleton field
        /// </summary>
        private static SinaService instance;

        /// <summary>
        /// gets puclic singleton property
        /// </summary>
        public static SinaService Instance => instance ?? (instance = new SinaService());

        /// <summary>
        /// Field for tracking oAuthTokens.
        /// </summary>
        private SinaOAuthTokens tokens;

        /// <summary>
        /// Field for tracking initialization status.
        /// </summary>
        private bool isInitialized;




        public bool Initialize(string AppKey, string AppSecret, string callBackUri)
        {
            if (string.IsNullOrEmpty(AppKey))
            {
                throw new ArgumentNullException(nameof(AppKey));
            }
            if (string.IsNullOrEmpty(AppSecret))
            {
                throw new ArgumentNullException(nameof(AppSecret));
            }
            if (string.IsNullOrEmpty(callBackUri))
            {
                throw new ArgumentNullException(nameof(callBackUri));
            }

            var oAuthTokens = new SinaOAuthTokens
            {
                AppKey = AppKey,
                AppSecret = AppSecret,
                CallbackUri = callBackUri
            };
            return Initialize(oAuthTokens);
        }

        public bool Initialize(SinaOAuthTokens oAuthTokens)
        {
            if (oAuthTokens == null)
            {
                throw new ArgumentNullException(nameof(oAuthTokens));
            }
            tokens = oAuthTokens;
            isInitialized = true;
            sinaDataProvider = null;

            return true;
        }

        public async Task<bool> LoginAsync()
        {
            return await Provider.LoginAsync();
        }

        //获取用户信息
        //get user info
        public async Task<SinaUser> GetUserAsync(string uid = null)
        {
            if (Provider.LoggedIn)
            {
                return await Provider.GetUserAsync(tokens.uid);
            }
            var isLoggedIn = await LoginAsync();
            if (isLoggedIn)
            {
                return await GetUserAsync(tokens.uid);
            }
            return null;
        }

        //获得用户发布的状态
        //get user status
        public async Task<UserStatus> GetUserTimeLineAsync()
        {
            if (Provider.LoggedIn)
            {
                return await Provider.GetUserTimeLineAsync();
            }
            if (await LoginAsync())
            {
                return await GetUserTimeLineAsync();
            }
            return null;
        }

        public async Task<bool> ShareStatusAsync(string text)
        {
            if (Provider.LoggedIn)
            {
                return await Provider.ShareStatusAsync(text);
            }
            if (await LoginAsync())
            {
                return await ShareStatusAsync(text);
            }
            return false;
        }

        public async Task<bool> ShareStatusWithPicture(string text, StorageFile file)
        {
            if (Provider.LoggedIn)
            {
                return await Provider.ShareStatusWithPicture(text, file);
            }
            if (await LoginAsync())
            {
                return await ShareStatusWithPicture(text, file);
            }
            return false;
        }

        //重置应用
        public void clear()
        {
            
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values.Clear();

            if (isInitialized)
            {
                Provider.clear();
            }
        }
    }
}
