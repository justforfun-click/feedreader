using FeedReader.WebClient.Datas;
using System;
using System.Threading.Tasks;

namespace FeedReader.WebClient.Services
{
    public class FeedService
    {
        private readonly ApiService _api;

        public event Action RefreshRequested;

        public FeedService(ApiService api)
        {
            _api = api;
        }

        public async Task RefreshFeedAsync(Feed feed)
        {
            var res = await _api.RefreshFeed(feed.Uri);
            if (string.IsNullOrWhiteSpace(feed.Name))
            {
                // Only update name if we don't have customized name.
                feed.Name = res.Name;
            }
            feed.IconUri = res.IconUri;
            feed.WebsiteLink = res.WebsiteLink;
            feed.Description = res.Description;
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
