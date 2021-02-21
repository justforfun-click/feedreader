using System.Collections.Generic;

namespace FeedReader.ClientCore.Models
{
    public class Feed : Share.DataContracts.Feed
    {
        public new List<FeedItem> Items { get; set; } = new List<FeedItem>();

        public bool IsActive { get; set; }

        public int NextItemsPage { get; set; }
    }
}
