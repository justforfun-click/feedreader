using System;

namespace FeedReader.Backend.Share.Entities
{
    public class FeedInfoEntity
    {
        public string Uri { get; set; }

        public string OriginalUri { get; set; }

        public string IconUri { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string WebsiteLink { get; set; }

        public string Category { get; set; }
    }

    public class FeedItemEntity
    {
        public string Title { get; set; }

        public string PermentLink { get; set; }

        public string TopicPictureUri { get; set; }

        public string Content { get; set; }

        public string Summary { get; set; }

        public DateTime PubDate { get; set; }

        public string Guid { get; set; }

        public FeedItemEntity()
        {
        }

        public FeedItemEntity(FeedItemEntity feedItem)
        {
            Title = feedItem.Title;
            PermentLink = feedItem.PermentLink;
            TopicPictureUri = feedItem.TopicPictureUri;
            Content = feedItem.Content;
            Summary = feedItem.Summary;
            PubDate = feedItem.PubDate;
            Guid = feedItem.Guid;
        }
    }

    public class FeedItemExEntity : FeedItemEntity
    {
        public string FeedUri { get; set; }

        public string FeedIconUri { get; set; }

        public string FeedName { get; set; }

        public string FeedCategory { get; set; }

        public FeedItemExEntity()
        {
        }

        public FeedItemExEntity(FeedItemEntity feedItem, ServerCore.Models.Feed feed)
            : base(feedItem)
        {
            FeedUri = feed.Uri;
            FeedIconUri = feed.IconUri;
            FeedName = feed.Name;
            FeedCategory = feed.Category;
        }
    }
}
