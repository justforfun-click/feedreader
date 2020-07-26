using System;

namespace FeedReader.Share.DataContracts
{
    public class FeedItem
    {
        public string Title { get; set; }

        public string PermentLink { get; set; }

        public string TopicPictureUri { get; set; }

        public string Channel { get; set; }

        public string Summary { get; set; }

        public string Content { get; set; }

        public DateTime PubDate { get; set; }

        public bool IsReaded { get; set; }
    }
}
