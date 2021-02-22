using FeedReader.Share.DataContracts;

namespace FeedReader.WebApi.Extensions
{
    static class FeedEntityExtensions
    {
        public static Feed ToFeed(this FeedParser.FeedInfo feedInfo)
        {
            return new Feed()
            {
                Description = feedInfo.Description,
                IconUri = feedInfo.IconUri,
                Name = feedInfo.Name,
                OriginalUri = feedInfo.OriginalUri,
                Uri = feedInfo.Uri,
                WebsiteLink = feedInfo.WebsiteLink,
            };
        }

        public static FeedItem ToFeedItem(this FeedParser.FeedItem feedItem)
        {
            return new FeedItem()
            {
                Content = feedItem.Content,
                PermentLink = feedItem.PermentLink,
                PubDate = feedItem.PubDate,
                Summary = feedItem.Summary,
                Title = feedItem.Title,
                TopicPictureUri = feedItem.TopicPictureUri,
            };
        }

        public static FeedItem CopyTo(this Backend.Share.Entities.FeedItemExEntity feedItemEx, FeedItem feedItem)
        {
            feedItem.Content = feedItemEx.Content;
            feedItem.FeedIconUri = feedItemEx.FeedIconUri;
            feedItem.FeedName = feedItemEx.FeedName;
            feedItem.FeedUri = feedItemEx.FeedUri;
            feedItem.PermentLink = feedItemEx.PermentLink;
            feedItem.PubDate = feedItemEx.PubDate;
            feedItem.Summary = feedItemEx.Summary;
            feedItem.Title = feedItemEx.Title;
            feedItem.TopicPictureUri = feedItemEx.TopicPictureUri;
            return feedItem;
        }
    }
}
