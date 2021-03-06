using System.Collections.Generic;

namespace FeedReader.ClientCore.Models
{
    public class User
    {
        public string Token { get; set; }

        public string Uuid { get; set; }

        public List<Feed> Feeds { get; set; }
    }
}
