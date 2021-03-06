using System.Collections.Generic;

namespace FeedReader.Share.DataContracts
{
    public class User
    {
        public string Token { get; set; }

        public string Uuid { get; set; }

        public List<Feed> Feeds { get; set; }
    }
}
