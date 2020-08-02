using FeedReader.WebClient.Datas;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            await _api.MarkAsReaded(new List<string> { feedItem.PermentLink });
            feedItem.IsReaded = true;
            RefreshRequested?.Invoke();
        }

        public async Task MarkFeedAllItemsAsReadedAsync(Feed feed)
        {
            var unReadedItems = feed.Items.Where(i => !i.IsReaded).ToList();
            if (unReadedItems.Count > 0)
            {
                unReadedItems.ForEach(i => i.IsReaded = true);
                RefreshRequested?.Invoke();
                try
                {
                    await _api.MarkAsReaded(unReadedItems.Select(i => i.PermentLink).ToList());
                }
                catch (Exception ex)
                {
                    _logger.Error($"Mark feed all items as readed feed, ex: {ex.Message}");
                    unReadedItems.ForEach(i => i.IsReaded = false);
                    RefreshRequested?.Invoke();
                }
            }
        }
    }
}
