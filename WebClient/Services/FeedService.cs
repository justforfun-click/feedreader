using FeedReader.WebClient.Datas;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeedCategory = FeedReader.Share.DataContracts.FeedCategory;

namespace FeedReader.WebClient.Services
{
    public class FeedService
    {
        private readonly ApiService _api;

        private readonly LogService _logger;

        public event Action RefreshRequested;

        public FeedService(ApiService api, LogService logger)
        {
            _api = api;
            _logger = logger;
        }

        public async Task RefreshFeedAsync(Feed feed, bool more = false)
        {
            var res = await _api.RefreshFeed(feed.Uri, more ? feed.NextRowKey : null);
            if (string.IsNullOrWhiteSpace(feed.Name))
            {
                // Only update name if we don't have customized name.
                feed.Name = res.Name;
            }
            feed.IconUri = res.IconUri;
            feed.WebsiteLink = res.WebsiteLink;
            feed.Description = res.Description;
            feed.Error = res.Error;
            feed.NextRowKey = res.NextRowKey;

            if (res.Items != null)
            {
                if (!more)
                {
                    feed.Items.Clear();
                }
                feed.Items.AddRange(res.Items);
            }
            RefreshRequested?.Invoke();
        }

        public void MarkFeedAllItemsAsReadedAsync(Feed feed)
        {
            if (feed.Items != null && feed.Items.Count > 0)
            {
                _ = _api.MarkAsReaded(feed.Uri, feed.Items[0].PubDate);
                feed.Items.ForEach(i => i.IsReaded = true);
                RefreshRequested?.Invoke();
            }
        }

        public Task<List<Feed>> GetFeedsByCategory(FeedCategory feedCategory)
        {
            return _api.GetFeedsByCategory(feedCategory);
        }
    }
}
