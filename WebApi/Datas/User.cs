using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FeedReader.WebApi.Datas
{
    public class User
    {
        public string Token { get; set; }

        public string Uuid { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public string Email { get; set; }

        public string AvatarUrl { get; set; }
    }
}
