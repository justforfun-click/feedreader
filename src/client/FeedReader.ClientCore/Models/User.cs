using System.Collections.Generic;

namespace FeedReader.ClientCore.Models
{
    public class User : Share.DataContracts.User
    {
        public new List<Feed> Feeds { get; set; }
    }
}
