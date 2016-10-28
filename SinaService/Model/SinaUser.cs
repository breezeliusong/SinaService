using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaService.Model
{
    public class SinaUser
    {
        /// <summary>
        /// gets or sets user Id
        /// </summary>
        [JsonProperty]
        public long id { get; set; }
        [JsonProperty]
        public string screen_name { get; set; }
        [JsonProperty]
        public string name { get; set; }
        [JsonProperty]
        public string profile_image_url { get; set; }
        [JsonProperty]
        public string location{ get; set; }
        [JsonProperty]
        public string description{ get; set; }
    }
}
