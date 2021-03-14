using FeedReader.ServerCore.Datas;
using FeedReader.ServerCore.Models;
using FeedReader.WebApi;
using FeedReader.WebApi.AdminFunctions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SolrNet;
using SolrNet.Commands.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Solrs = FeedReader.ServerCore.Models.Solrs;

namespace FeedReader.ServerCore.Services
{
    public interface IFeedService
    {
        Task UpdateFeeds(CancellationToken cancellationToken);
        Task<List<FeedItem>> GetCategoryFeedItems(FeedCategory category, int page);
        Task<List<FeedItem>> Search(string keywords, int page);
    }

    class FeedService : IFeedService
    {
        private readonly IDbContextFactory<FeedReaderDbContext> _dbFactory;
        private readonly IDistributedCache _remoteCache;
        private readonly IMemoryCache _memCache;
        private readonly ILogger _logger;
        private readonly ISolrOperations<Solrs.FeedItem> _solrFeedItems;

        public FeedService(IDbContextFactory<FeedReaderDbContext> dbFactory, ISolrOperations<Solrs.FeedItem> solrFeedItems, IDistributedCache remoteCache, IMemoryCache memCache, ILogger<FeedService> logger)
        {
            _dbFactory = dbFactory;
            _remoteCache = remoteCache;
            _memCache = memCache;
            _logger = logger;
            _solrFeedItems = solrFeedItems;
        }

        /// <summary>
        /// Update all feeds. Refresh category feed items remote cache.
        /// </summary>
        public async Task UpdateFeeds(CancellationToken cancellationToken)
        {
            // Update all feeds first.
            try
            {
                await UpdateFeedFunc.UpdateFeeds(_dbFactory, _solrFeedItems, cancellationToken, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateFeeds failed, ex: {ex.Message}");
            }

            // Refresh feed category cache.
            foreach (var category in Enum.GetValues<FeedCategory>())
            {
                try
                {
                    var feedItems = await GetCategoryFeedItemsFromDb(category, skip: 0, take: 1000);

                    // Save to cache.
                    await _remoteCache.SetStringAsync(Consts.CACHE_KEY_LATEST_FEEDITEMS_CATEGORY_PREFIX + category.ToString(), JsonSerializer.Serialize(feedItems));
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Update feed items cache for category {category} failed, ex: {ex.Message}");
                }
            }
        }

        public async Task<List<FeedItem>> GetCategoryFeedItems(FeedCategory category, int page)
        {
            if (page * 50 >= 1000)
            {
                // Remote cache only has 1000 items. Fetch from db directly.
                return await GetCategoryFeedItemsFromDb(category, skip: page * 50, take: 50);
            }

            // Try to get from local cache first.
            List<FeedItem> items;
            var key = Consts.CACHE_KEY_LATEST_FEEDITEMS_CATEGORY_PREFIX + category.ToString();
            if (!_memCache.TryGetValue<List<FeedItem>>(key, out items))
            {
                // Can't find from local cache, try to get from remote cache.
                var content = await _remoteCache.GetStringAsync(key);
                if (string.IsNullOrEmpty(content))
                {
                    // Remote cache doesn't have. Get from db directly.
                    items = await GetCategoryFeedItemsFromDb(category, skip: 0, take: 1000);

                    // Save to remote cache.
                    await _remoteCache.SetStringAsync(key, JsonSerializer.Serialize(items));
                }
                else
                {
                    items = JsonSerializer.Deserialize<List<FeedItem>>(content);
                }

                // Save to local cache.
                _memCache.Set(key, items, absoluteExpirationRelativeToNow: TimeSpan.FromMinutes(1));
            }

            return items.Skip(page * 50).Take(50).ToList();
        }

        public async Task<List<FeedItem>> Search(string keywords, int page)
        {
            var res = await _solrFeedItems.QueryAsync(new SolrQuery($"content:{keywords}"), new QueryOptions
            {
                OrderBy = new [] { new SortOrder("publish_time_in_utc", Order.DESC) },
                StartOrCursor = new StartOrCursor.Start(page * 50),
                Rows = 50
            });
            return res.Select(s => SolrFeedItemToFeedItem(s)).ToList();
        }

        private async Task<List<FeedItem>> GetCategoryFeedItemsFromDb(FeedCategory category, int skip, int take)
        {
            var db = _dbFactory.CreateDbContext();
            List<ServerCore.Models.FeedItem> feedItems;
            if (category == FeedCategory.Default)
            {
                feedItems = await db.FeedItems
                    .Include(f => f.Feed)
                    .Where(f => f.Feed.Category == "Default" || string.IsNullOrEmpty(f.Feed.Category))
                    .OrderByDescending(f => f.PublishTimeInUtc)
                    .Skip(skip)
                    .Take(take).ToListAsync();
            }
            else
            {
                feedItems = await db.FeedItems
                    .Include(f => f.Feed)
                    .Where(f => f.Feed.Category == category.ToString())
                    .OrderByDescending(f => f.PublishTimeInUtc)
                    .Skip(skip)
                    .Take(take).ToListAsync();
            }
            return feedItems;
        }


        private FeedItem SolrFeedItemToFeedItem(Solrs.FeedItem s)
        {
            var feedItem = new FeedItem
            {
                FeedId = s.FeedId.FirstOrDefault(),
                Feed = new Feed
                {
                    Id = s.FeedId.FirstOrDefault(),
                    IconUri = s.FeedIconUri?.FirstOrDefault(),
                    Category = s.FeedCategory?.FirstOrDefault(),
                    Name = s.FeedName?.FirstOrDefault(),
                },
                Summary = s.Summary?.FirstOrDefault(),
                Content = s.Content?.FirstOrDefault(),
                Title = s.Title?.FirstOrDefault(),
                Id = s.Id,
                PublishTimeInUtc = s.PublishTimeInUtc.FirstOrDefault(),
                TopicPictureUri = s.TopicPictureUri?.FirstOrDefault(),
                Uri = s.Uri.FirstOrDefault(),
            };
            return feedItem;
        }
    }
}
