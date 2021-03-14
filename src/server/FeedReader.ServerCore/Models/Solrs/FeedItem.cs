using SolrNet.Attributes;
using System;
using System.Collections.Generic;

namespace FeedReader.ServerCore.Models.Solrs
{
    public class FeedItem
    {
        [SolrUniqueKey("id")]
        public string Id { get; set; }

        [SolrField("uri")]
        public ICollection<string> Uri { get; set; }

        [SolrField("publish_time_in_utc")]
        public ICollection<DateTime> PublishTimeInUtc { get; set; }

        [SolrField("summary")]
        public ICollection<string> Summary { get; set; }

        [SolrField("content")]
        public ICollection<string> Content { get; set; }

        [SolrField("title")]
        public ICollection<string> Title { get; set; }

        [SolrField("topic_picture_uri")]
        public ICollection<string> TopicPictureUri { get; set; }

        [SolrField("feed_id")]
        public ICollection<string> FeedId { get; set; }

        [SolrField("feed_name")]
        public ICollection<string> FeedName { get; set; }

        [SolrField("feed_category")]
        public ICollection<string> FeedCategory { get; set; }

        [SolrField("feed_icon_uri")]
        public ICollection<string> FeedIconUri { get; set; }

        public FeedItem()
        {
        }

        public FeedItem(Models.FeedItem feedItem, Models.Feed feed)
        {
            Id = feedItem.Id;
            Uri = new List<string> { feedItem.Uri };
            PublishTimeInUtc = new List<DateTime> { feedItem.PublishTimeInUtc };
            Summary = new List<string> { Utils.RemoveHtmlTag(feedItem.Summary) };
            Content = new List<string> { Utils.RemoveHtmlTag(feedItem.Content) };
            Title = new List<string> { Utils.RemoveHtmlTag(feedItem.Title) };
            TopicPictureUri = new List<string> { feedItem.TopicPictureUri };
            FeedId = new List<string> { feed.Id };
            FeedName = new List<string> { feed.Name };
            FeedCategory = new List<string> { feed.Category };
            FeedIconUri = new List<string> { feed.IconUri };
        }
    }
}
