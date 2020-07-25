using System;
using System.Threading.Tasks;
using FeedReader.WebClient.Models;

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

        public async Task RefreshFeedAsync(Feed feed)
        {
            var res = await _api.RefreshFeed(feed.Uri);
            if (!string.IsNullOrWhiteSpace(res.Name) && (string.IsNullOrWhiteSpace(feed.Name) || feed.Name == feed.Uri))
            {
                feed.Name = res.Name;
            }

            if (res.Items != null)
            {
                feed.Items.Clear();
                feed.Items.AddRange(res.Items);
            }
            RefreshRequested?.Invoke();
        }
    }
}
