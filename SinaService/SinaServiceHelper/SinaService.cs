using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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



        public bool Initialize(string AppKey,String AppSecret,string callBackUri)
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

        public async void GetUserAsync()
        {

        }
    }
}
