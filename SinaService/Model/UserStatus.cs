using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SinaService.Model
{
    public class UserStatus
    {
        [JsonProperty]
        public List<Status> statuses { get; set; }
    }

    public class Status
    {
        [JsonProperty]
        public string created_at { get; set; }
        [JsonProperty]
        public string text { get; set; }
        [JsonProperty]
        public string thumbnail_pic { get; set; }
    }
}
