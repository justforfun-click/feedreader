using FeedReader.Backend.Share.FeedParsers;
using FeedReader.ServerCore;
using FeedReader.ServerCore.Datas;
using FeedReader.Share;
using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Extensions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using User = FeedReader.ServerCore.Models.User;

namespace FeedReader.WebApi.Processors
{
    public class FeedProcessor
    {
        const int MAX_RETURN_COUNT = 50;

        private IDbContextFactory<FeedReaderDbContext> _dbFactory;
        private readonly HttpClient _httpClient;

        public FeedProcessor(IDbContextFactory<FeedReaderDbContext> dbFactory, HttpClient httpClient = null)
        {
            _dbFactory = dbFactory;
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<Feed> RefreshFeedAsync(string uri, bool noItems = false)
        {
            uri = uri.Trim();
            Feed feed = null;
            try
            {
                var parser = FeedParser.Create(await _httpClient.GetStringAsync(uri));
                feed = parser.ParseFeedInfo().ToFeed();
                if (!noItems)
                {
                    feed.Items = parser.ParseFeedItems().Select(i => i.ToFeedItem()).ToList();
                }
            }
            catch (HttpRequestException)
            {
                feed = new Feed()
                {
                    Error = "The feed uri is not reachable."
                };
            }
            catch (XmlException)
            {
                feed = new Feed()
                {
                    Error = "The feed content is not valid."
                };
            }
            feed.Uri = uri.Trim().ToLower();
            feed.OriginalUri = uri;
            return feed;
        }

        public async Task<Feed> GetFeedItemsAsync(string feedUri, int page, User user)
        {
            var originalUri = feedUri;
            feedUri = feedUri.Trim().ToLower();
            var feedId = Utils.Sha256(feedUri);
            var db = _dbFactory.CreateDbContext();

            // Get feed information.
            // If the user blob is not null, we will get the feed from the user blob because user might customize the group and name on this feed.
            ServerCore.Models.UserFeed userFeed = null;
            ServerCore.Models.Feed feedInDb = null;
            if (user != null)
            {
                userFeed = await db.UserFeeds.Include(f => f.Feed).FirstOrDefaultAsync(u => u.UserId == user.Id && u.FeedId == feedId);
                feedInDb = userFeed?.Feed;
            }

            // If we didn't get the feed, two possibility:
            // 1. The feed is not subscribed by user yet.
            // 2. Anonymous user.
            // No matter for which case, we will try to get the feed info from feed info table directly.
            if (feedInDb == null)
            {
                feedInDb = await db.Feeds.FindAsync(feedId);
            }

            if (feedInDb == null)
            {
                throw new ExternalErrorExcepiton($"Feed '{feedUri}' is not found.");
            }

            var feed = new Feed
            {
                Description = feedInDb.Description,
                IconUri = feedInDb.IconUri,
                Name = feedInDb.Name,
                OriginalUri = feedInDb.Uri,
                Uri = feedUri,
                WebsiteLink = feedInDb.WebSiteUri,
            };
            if (userFeed != null)
            {
                feed.Group = userFeed.Group;
            }

            // Get feed items.
            var feedItems = await db.FeedItems
                .Where(f => f.FeedId == feedId)
                .OrderByDescending(f => f.PublishTimeInUtc)
                .Skip(page * 50)
                .Take(50).ToListAsync();
            if (feedItems.Count > 0)
            {
                feed.Items = feedItems.Select(f => new FeedItem
                {
                    Summary = f.Summary,
                    Content = f.Content,
                    PermentLink = f.Uri,
                    PubDate = f.PublishTimeInUtc,
                    Title = f.Title,
                    TopicPictureUri = f.TopicPictureUri,
                }).ToList();
            }
            else if (page == 0)
            {
                // We haven't refreshed this feed on the server (new added?), get it directly form rss.
                feed = await RefreshFeedAsync(string.IsNullOrWhiteSpace(feed.OriginalUri) ? feed.Uri : feed.OriginalUri);
            }

            // Mark readed or not.
            if (userFeed != null && userFeed.LastReadedTimeInUtc.Ticks != 0)
            {
                foreach (var feedItem in feed.Items)
                {
                    if (feedItem.PubDate <= userFeed.LastReadedTimeInUtc)
                    {
                        feedItem.IsReaded = true;
                    }
                }
            }

            // Mark stared or not.
            if (user != null)
            {
                // Mark stared or not
                var staredHashs = db.UserFavorites.Where(f => f.UserId == user.Id).Select(f => f.FavoriteItemIdHash).ToList();
                if (staredHashs.Count > 0)
                {
                    foreach (var feedItem in feed.Items)
                    {
                        if (staredHashs.Contains(Share.Utils.Md5(feedItem.PermentLink)))
                        {
                            feedItem.IsStared = true;
                        }
                    }
                }
            }

            // All done.
            return feed;
        }

        public async Task RefreshFeedAsync(string feedOriginalUri, CloudTable feedTable, CloudTable feedItemTable)
        {
            await SaveFeedAsync(await RefreshFeedAsync(feedOriginalUri), feedTable, feedItemTable);
        }

        public async Task<Feed> SubscribeFeedAsync(string feedOriginalUri, string customGroup, User user)
        {
            Feed feed = null;
            string feedUriHash = null;
            var db = _dbFactory.CreateDbContext();
            for (var i = 0; i < 10; ++i)
            {
                var feedUri = feedOriginalUri.Trim().ToLower();
                feedUriHash = Utils.Sha256(feedUri);

                // Do we have this feed already?
                var feedInDb = await db.Feeds.FindAsync(feedUriHash);
                if (feedInDb == null)
                {
                    // Get information of this feed.
                    feed = await RefreshFeedAsync(feedOriginalUri, noItems: true);
                    if (feed.Error != null)
                    {
                        // This uri may not be the feed uri. It maybe the webpage, let's try to find out the potential feed uri in this webpage.
                        feedOriginalUri = await DiscoverFeedUriAsync(feedOriginalUri);
                        if (string.IsNullOrWhiteSpace(feedOriginalUri))
                        {
                            break;
                        }
                        continue;
                    }

                    // Save to feed table.
                    db.Feeds.Add(new ServerCore.Models.Feed
                    {
                        Description = feed.Description,
                        IconUri = feed.IconUri,
                        Id = feedUriHash,
                        Name = feed.Name,
                        RegistrationTimeInUtc = DateTime.UtcNow,
                        WebSiteUri = feed.WebsiteLink,
                        Uri = feed.OriginalUri,
                    });

                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (ex.IsUniqueConstraintException())
                    {
                        // Save to ignore.
                    }

                    break;
                }
                else
                {
                    feed = new Feed
                    {
                        Description = feedInDb.Description,
                        IconUri = feedInDb.IconUri,
                        Name = feedInDb.Name,
                        OriginalUri = feedInDb.Uri,
                        Uri = feedUri,
                        WebsiteLink = feedInDb.WebSiteUri,
                    };
                }
            }

            Debug.Assert(feed != null);
            Debug.Assert(feedUriHash != null);

            // User user customized group.
            feed.Group = customGroup;

            // Save to usersfeeds table.
            db.UserFeeds.Add(new ServerCore.Models.UserFeed
            {
                UserId = user.Id,
                FeedId = feedUriHash
            });
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintException())
            {
                // Save to ignore.
            }

            // Return
            return feed;
        }

        public async Task UnsubscribeFeedAsync(string feedUri, User user)
        {
            var feedId = Utils.Sha256(feedUri);
            var userFeed = new ServerCore.Models.UserFeed
            {
                UserId = user.Id,
                FeedId = feedId
            };
            var db = _dbFactory.CreateDbContext();
            db.UserFeeds.Attach(userFeed);
            db.UserFeeds.Remove(userFeed);
            await db.SaveChangesAsync();
        }

        public async Task<List<Feed>> GetFeedsByCategory(FeedCategory category, CloudTable feedTable, CloudTable feedItemTable)
        {
            // Generate filter.
            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "feed_info");
            if (category != FeedCategory.Recommended)
            {
                filter = TableQuery.CombineFilters(filter, TableOperators.And, TableQuery.GenerateFilterCondition("Category", QueryComparisons.Equal, category.ToString()));
            }

            var queryRes = await feedTable.ExecuteQuerySegmentedAsync(new TableQuery<FeedInfoEntity>()
            {
                TakeCount = 10,
                FilterString = filter
            }, null);

            var feeds = new List<Feed>();
            if (queryRes != null && queryRes.Results != null)
            {
                feeds = queryRes.Results.Select(f => f.CopyTo(new Feed())).ToList();
            }

            var tuples = new List<Tuple<Feed, FeedItem>>();
            foreach (var feed in feeds)
            {
                var itemQueryRes = await feedItemTable.ExecuteQuerySegmentedAsync(new TableQuery<FeedItemEntity>()
                {
                    TakeCount = MAX_RETURN_COUNT,
                    FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Utils.Sha256(feed.Uri))
                }, null);

                if (itemQueryRes != null && itemQueryRes.Results != null)
                {
                    tuples.AddRange(itemQueryRes.Results.Select(i => Tuple.Create(feed, i.CopyTo(new FeedItem()))).ToList());
                }
            }

            tuples = tuples.OrderByDescending(t => t.Item2.PubDate).ToList();
            feeds = tuples.Select(t => t.Item1).Distinct().ToList();
            foreach (var feed in feeds)
            {
                feed.Items = tuples.Where(t => t.Item1 == feed).Select(t => t.Item2).ToList();
            }

            int leftCount = MAX_RETURN_COUNT;
            feeds = feeds.OrderBy(f => f.Items.Count).ToList();
            for (int i = 0; leftCount > 0 && i < feeds.Count; ++i)
            {
                int avg = Math.Max(leftCount / (feeds.Count - i), 1);
                if (feeds[i].Items.Count <= avg)
                {
                    // Use all items in this feed, check the next feed.
                    leftCount -= feeds[i].Items.Count;
                }
                else
                {
                    feeds[i].Items.RemoveRange(avg, feeds[i].Items.Count - avg);
                    leftCount -= avg;
                }
            }
            return feeds;
        }

        public async Task<List<FeedItem>> GetFeedItemsByCategory(FeedCategory category, int page)
        {
            var db = _dbFactory.CreateDbContext();
            List<ServerCore.Models.FeedItem> feedItems;
            if (category == FeedCategory.Recommended)
            {
                feedItems = await db.FeedItems
                    .Include(f => f.Feed)
                    .Where(f => f.Feed.Category == "Default" || string.IsNullOrEmpty(f.Feed.Category))
                    .OrderByDescending(f => f.PublishTimeInUtc)
                    .Skip(page * 50)
                    .Take(50).ToListAsync();
            }
            else
            {
                feedItems = await db.FeedItems
                    .Include(f => f.Feed)
                    .Where(f => f.Feed.Category == category.ToString())
                    .OrderByDescending(f => f.PublishTimeInUtc)
                    .Skip(page * 50)
                    .Take(50).ToListAsync();
            }

            return feedItems.Select(f => new FeedItem
            {
                Summary = f.Summary,
                Content = f.Content,
                FeedIconUri = f.Feed.IconUri,
                FeedName = f.Feed.Name,
                FeedUri = f.Feed.Uri,
                PermentLink = f.Uri,
                PubDate = f.PublishTimeInUtc,
                Title = f.Title,
                TopicPictureUri = f.TopicPictureUri,
            }).ToList();
        }

        public async Task UpdateFeedAsync(string feedUri, string newFeedGroup, User user)
        {
            // Get the original feed.
            var feedId = Utils.Sha256(feedUri);
            var db = _dbFactory.CreateDbContext();
            var userFeed = await db.UserFeeds.FindAsync(user.Id, feedId);
            if (userFeed == null)
            { 
                throw new ExternalErrorExceptionNotFound();
            }

            userFeed.Group = newFeedGroup;
            await db.SaveChangesAsync();
        }

        private async Task SaveFeedAsync(Feed feed, CloudTable feedTable, CloudTable feedItemTable)
        {
            // Update feed info into feed table.
            var feedUri = feed.Uri.Trim().ToLower();
            await feedTable.ExecuteAsync(TableOperation.InsertOrMerge(new FeedInfoEntity()
            {
                PartitionKey = "feed_info",
                RowKey = Utils.Sha256(feedUri),
                Uri = feedUri,
                OriginalUri = feed.OriginalUri,
                Name = feed.Name,
                Description = feed.Description,
                IconUri = feed.IconUri,
                WebsiteLink = feed.WebsiteLink,
            }));

            // Save feed items
            if (feed.Items != null)
            {
                var partitionKey = Utils.Sha256(feed.Uri.Trim().ToLower());
                foreach (var item in feed.Items)
                {
                    var itemUri = item.PermentLink.Trim();
                    await feedItemTable.ExecuteAsync(TableOperation.InsertOrMerge(new FeedItemEntity()
                    {
                        PartitionKey = partitionKey,
                        RowKey = $"{string.Format("{0:D19}", DateTime.MaxValue.Ticks - item.PubDate.ToUniversalTime().Ticks)}-{Utils.Sha256(itemUri)}",
                        PermentLink = itemUri,
                        Title = item.Title,
                        Summary = item.Summary,
                        Content = item.Content,
                        PubDate = item.PubDate,
                        TopicPictureUri = item.TopicPictureUri,
                    }));
                }
            }
        }

        private async Task<string> DiscoverFeedUriAsync(string uri)
        {
            try
            {
                var content = await _httpClient.GetStringAsync(uri);
                foreach (var regex in HtmlFeedRegexes)
                {
                    var match = regex.Match(content);
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
                return null;
            }
            catch
            {
                // Ignore all erros.
                return null;
            }
        }

        private static Regex[] HtmlFeedRegexes;
        
        static FeedProcessor()
        {
            HtmlFeedRegexes = new Regex[]
            {
                new Regex("<link [^>]*type\\s*=\\s*['\"]application\\/rss\\+xml['\"][^>]*href\\s*=\\s*['\"]([^'\"]*)['\"]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex("<link [^>]*href\\s*=\\s*['\"]([^'\"]*)['\"][^>]*type\\s*=\\s*['\"]application\\/rss\\+xml['\"]", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            };
        } 
    }
}
