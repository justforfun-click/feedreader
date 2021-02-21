using FeedReader.Share.DataContracts;
using System;

namespace FeedReader.WebApi.Entities
{
    public class FeedInfoEntity
    {
        public string Uri { get; set; }

        public string OriginalUri { get; set; }

        public string IconUri { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string WebsiteLink { get; set; }

        public FeedInfoEntity()
        {
        }

        public FeedInfoEntity(Feed feed)
        {
            Uri = feed.Uri;
            OriginalUri = feed.OriginalUri;
            IconUri = feed.IconUri;
            Name = feed.Name;
            Description = feed.Description;
            WebsiteLink = feed.WebsiteLink;
        }

        public virtual Feed CopyTo(Feed feed)
        {
            feed.Uri = Uri;
            feed.OriginalUri = OriginalUri;
            feed.IconUri = IconUri;
            feed.Description = Description;
            feed.WebsiteLink = WebsiteLink;

            if (string.IsNullOrWhiteSpace(feed.Name))
            {
                feed.Name = Name;
            }

            return feed;
        }
    }

    public class FeedItemEntity
    {
        public string Title { get; set; }

        public string PermentLink { get; set; }

        public string TopicPictureUri { get; set; }

        public string Content { get; set; }

        public string Summary { get; set; }

        public DateTime PubDate { get; set; }

        public FeedItem CopyTo(FeedItem feedItem)
        {
            feedItem.Title = Title;
            feedItem.PermentLink = PermentLink;
            feedItem.TopicPictureUri = TopicPictureUri;
            feedItem.Content = Content;
            feedItem.Summary = Summary;
            feedItem.PubDate = PubDate;
            return feedItem;
        }
    }

    public class UserFeedEntity : FeedInfoEntity
    {
        public string Group { get; set; }

        public DateTime? LastReadedTime { get; set; }

        public override Feed CopyTo(Feed feed)
        {
            base.CopyTo(feed);
            feed.Group = Group;
            return feed;
        }

        public UserFeedEntity()
        {
        }

        public UserFeedEntity(Feed feed)
        {
            Group = feed.Group;
        }
    }
}
