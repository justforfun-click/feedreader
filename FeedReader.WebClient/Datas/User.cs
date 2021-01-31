using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FeedReader.WebClient.Datas
{
    public class User : Share.DataContracts.User
    {
        public new List<Feed> Feeds { get; set; }
    }
}
