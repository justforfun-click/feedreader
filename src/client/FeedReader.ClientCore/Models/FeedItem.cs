using System;
using System.Collections.Generic;

namespace FeedReader.ClientCore.Models
{
    public class FeedItem
    {
        public string Title { get; set; }

        public string PermentLink { get; set; }

        public string TopicPictureUri { get; set; }

        public string Summary { get; set; }

        public string Content { get; set; }

        public DateTime PubDate { get; set; }

        public bool IsReaded { get; set; }

        public bool IsStared { get; set; }

        #region Return only query by category
        public string FeedUri { get; set; }

        public string FeedIconUri { get; set; }

        public string FeedName { get; set; }
        #endregion
    }

    public class FeedItems
    {
        public List<FeedItem> Items { get; set; } = new List<FeedItem>();

        public int NextPage { get; set; }
    }
}
