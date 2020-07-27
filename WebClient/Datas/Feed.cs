using System.Collections.Generic;

namespace FeedReader.WebClient.Datas
{
    public class Feed : Share.DataContracts.Feed
    {
        public new List<FeedItem> Items { get; set; } = new List<FeedItem>();

        public bool IsActive { get; set; }
    }
}
