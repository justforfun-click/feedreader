using System.Collections.Generic;

namespace FeedReader.ClientCore.Models
{
    public class FeedItem : Share.DataContracts.FeedItem
    {
    }

    public class FeedItems
    {
        public List<FeedItem> Items { get; set; }

        public int NextPage { get; set; }
    }
}
