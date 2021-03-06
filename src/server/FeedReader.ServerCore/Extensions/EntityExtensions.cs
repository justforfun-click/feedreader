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
    }
}
