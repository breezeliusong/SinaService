﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaService.SinaServiceHelper
{
    public class SinaOAuthTokens
    {
        public string AppKey { get; set; }
        public string AppSecret { get; set; }
        public string CallbackUri { get; set; }
        public string AccessToken { get; set; }
        public string uid { get; set; }
        public string expires_in { get; set; }
        public bool isValid { get; set; }
    }
}
