using FeedReader.ClientCore.Models;
using System.Linq;
using System.Threading.Tasks;
using static FeedReader.Protos.FeedReaderServerApi;

namespace FeedReader.ClientCore
{
    public class FeedReaderClient
    {
        private FeedReaderServerApiClient _api;

        public void SetApiClient(FeedReaderServerApiClient api)
        {
            _api = api;
        }

        public async Task<FeedItems> Search(string keywords, int page)
        {
            var items = await _api.SearchAsync(new Protos.SearchRequest
            {
                KeyWords = keywords,
                Page = page
            });
            return new FeedItems
            {
                Items = items.FeedItems.Select(f => GetFeedItem(f)).ToList(),
                NextPage = 0
            };
        }

        private FeedItem GetFeedItem(Protos.FeedItemMessage f)
        {
            return new FeedItem
            {
                Content = f.Content,
                IsReaded = f.IsReaded,
                IsStared = f.IsStared,
                PermentLink = f.PermentLink,
                PubDate = f.PubDate.ToDateTime(),
                Summary = f.Summary,
                Title = f.Title,
                TopicPictureUri = f.TopicPictureUri
            };
        }

        private FeedItem GetFeedItem(Protos.FeedItemMessageWithFeedInfo f)
        {
            var feedItem = GetFeedItem(f.FeedItem);
            feedItem.FeedUri = f.FeedUri;
            feedItem.FeedIconUri = f.FeedIconUri;
            feedItem.FeedName = f.FeedName;
            return feedItem;
        }
    }
}
