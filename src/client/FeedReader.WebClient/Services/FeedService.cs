using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeedReader.ClientCore.Models;
using FeedReader.Protos;

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
            var res = await _api.RefreshFeed(feed.Uri, more ? feed.Items.NextPage : 0);
            if (string.IsNullOrWhiteSpace(feed.Name))
            {
                // Only update name if we don't have customized name.
                feed.Name = res.Name;
            }
            feed.IconUri = res.IconUri;
            feed.WebsiteLink = res.WebsiteLink;
            feed.Description = res.Description;
            feed.Error = res.Error;
            feed.Items.NextPage = res.Items.NextPage;

            if (res.Items != null)
            {
                if (!more)
                {
                    feed.Items.Items.Clear();
                }
                feed.Items.Items.AddRange(res.Items.Items);
            }
            RefreshRequested?.Invoke();
        }

        public void MarkFeedAllItemsAsReadedAsync(Feed feed)
        {
            if (feed.Items != null && feed.Items.Items.Count > 0)
            {
                _ = _api.MarkAsReaded(feed.Uri, feed.Items.Items[0].PubDate);
                feed.Items.Items.ForEach(i => i.IsReaded = true);
                RefreshRequested?.Invoke();
            }
        }

        public Task<List<FeedItem>> GetFeedItemsByCategory(FeedCategory feedCategory, int page)
        {
            return _api.GetFeedItemsByCategory(feedCategory, page);
        }
    }
}
