using SolrNet.Attributes;
using System;

namespace FeedReader.ServerCore.Models.Solrs
{
    public class FeedItem
    {
        [SolrUniqueKey("id")]
        public string Id { get; set; }

        [SolrField("uri")]
        public string Uri { get; set; }

        [SolrField("publish_time_in_utc")]
        public DateTime PublishTimeInUtc { get; set; }

        [SolrField("summary")]
        public string Summary { get; set; }

        [SolrField("content")]
        public string Content { get; set; }

        [SolrField("title")]
        public string Title { get; set; }

        [SolrField("topic_picture_uri")]
        public string TopicPictureUri { get; set; }

        [SolrField("feed_id")]
        public string FeedId { get; set; }

        [SolrField("feed_name")]
        public string FeedName { get; set; }

        [SolrField("feed_category")]
        public string FeedCategory { get; set; }

        [SolrField("feed_icon_uri")]
        public string FeedIconUri { get; set; }

        public FeedItem()
        {
        }

        public FeedItem(Models.FeedItem feedItem, Models.Feed feed)
        {
            Id = feedItem.Id;
            Uri = feedItem.Uri;
            PublishTimeInUtc = feedItem.PublishTimeInUtc;
            Summary = Utils.RemoveHtmlTag(feedItem.Summary);
            Content = Utils.RemoveHtmlTag(feedItem.Content);
            Title = Utils.RemoveHtmlTag(feedItem.Title);
            TopicPictureUri = feedItem.TopicPictureUri;
            FeedId = feed.Id;
            FeedName = feed.Name;
            FeedCategory = feed.Category;
            FeedIconUri = feed.IconUri;
        }
    }
}
