using System;

namespace FeedReader.Share.DataContracts
{
    public class FeedItem
    {
        public string Title { get; set; }

        public string PermentLink { get; set; }

        public string Content { get; set; }

        public DateTime PubDate { get; set; }
    }
}
