using System;
using System.Threading.Tasks;
using FeedReader.Share.DataContracts;

namespace FeedReader.WebClient.Services
{
    class FeedService
    {
        private readonly ApiService _api;

        public event Action RefreshRequested;

        public FeedService(ApiService api)
        {
            _api = api;
        }

        public async Task RefreshFeedAsync(Models.Feed feed)
        {
            var res = await _api.RefreshFeed(feed.Uri);
            if (!string.IsNullOrWhiteSpace(res.Name) && (string.IsNullOrWhiteSpace(feed.Name) || feed.Name == feed.Uri))
            {
                feed.Name = res.Name;
            }

            feed.Error = res.Error;

            if (res.Items != null)
            {
                feed.Items.Clear();
                feed.Items.AddRange(res.Items);
            }
            RefreshRequested?.Invoke();
        }

        public async Task MarkAsReadedAsync(FeedItem feedItem)
        {
            feedItem.IsReaded = true;
            try
            {
                await _api.MarkAsReaded(feedItem.PermentLink);
            }
            catch
            {
                feedItem.IsReaded = false;
            }
            RefreshRequested?.Invoke();
        }
    }
}
