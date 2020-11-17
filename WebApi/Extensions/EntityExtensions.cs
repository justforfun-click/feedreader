using FeedReader.Backend.Share.Entities;
using FeedReader.Share.DataContracts;

namespace FeedReader.WebApi.Extensions
{
    static class FeedEntityExtensions
    {
        public static Feed ToFeed(this FeedInfoEntity feedInfoEntity)
        {
            return new Feed()
            {
                Description = feedInfoEntity.Description,
                IconUri = feedInfoEntity.IconUri,
                Name = feedInfoEntity.Name,
                OriginalUri = feedInfoEntity.OriginalUri,
                Uri = feedInfoEntity.Uri,
                WebsiteLink = feedInfoEntity.WebsiteLink,
            };
        }

        public static FeedItem ToFeedItem(this FeedItemEntity feedItemEntity)
        {
            return new FeedItem()
            {
                Content = feedItemEntity.Content,
                PermentLink = feedItemEntity.PermentLink,
                PubDate = feedItemEntity.PubDate,
                Summary = feedItemEntity.Summary,
                Title = feedItemEntity.Title,
                TopicPictureUri = feedItemEntity.TopicPictureUri,
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
