using Azure.Storage.Blobs;
using FeedReader.Backend.Share.FeedParsers;
using FeedReader.Share;
using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Extensions;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace FeedReader.WebApi.Processors
{
    public class FeedProcessor
    {
        const int MAX_RETURN_COUNT = 50;

        private readonly HttpClient _httpClient;

        public FeedProcessor(HttpClient httpClient = null)
        {
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

        public async Task<Feed> GetFeedItemsAsync(string feedUri, string nextRowKey, BlobClient userBlob, CloudTable usersFeedsTable, CloudTable feedsTable, CloudTable feedItemsTable)
        {
            var originalUri = feedUri;
            feedUri = feedUri.Trim().ToLower();
            var feedUriHash = Utils.Sha256(feedUri);

            // Get feed information.
            // If the user blob is not null, we will get the feed from the user blob because user might customize the group and name on this feed.
            UserFeedEntity userFeedEntity = null;
            Feed feed = null;
            if (userBlob != null)
            {
                var res = await usersFeedsTable.ExecuteAsync(TableOperation.Retrieve<UserFeedEntity>(partitionKey: userBlob.Name, rowkey: feedUriHash));
                if (res?.Result != null)
                {
                    userFeedEntity = (UserFeedEntity)res.Result;
                    feed = userFeedEntity.CopyTo(new Feed());
                }
            }

            // If we didn't get the feed, two possibility:
            // 1. The feed is not subscribed by user yet.
            // 2. Anonymous user.
            // No matter for which case, we will try to get the feed info from feed info table directly.
            if (feed == null)
            {
                var res = await feedsTable.ExecuteAsync(TableOperation.Retrieve<FeedInfoEntity>(partitionKey: "feed_info", rowkey: feedUriHash));
                if (res?.Result != null)
                {
                    feed = ((FeedInfoEntity)res.Result).CopyTo(new Feed());
                }
                else
                {
                    feed = new Feed()
                    {
                        Uri = feedUri,
                        OriginalUri = originalUri,
                    };
                }
            }

            // Get feed items.
            var partitionKey = feedUriHash;
            TableContinuationToken token = null;
            if (!string.IsNullOrWhiteSpace(nextRowKey))
            {
                token = new TableContinuationToken()
                {
                    NextPartitionKey = partitionKey,
                    NextRowKey = nextRowKey
                };
            }

            var queryRes = await feedItemsTable.ExecuteQuerySegmentedAsync(new TableQuery<FeedItemEntity>()
            {
                TakeCount = MAX_RETURN_COUNT * 2,
                FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey)
            }, token);

            if (queryRes != null && queryRes.Results != null && queryRes.Results.Count > 0)
            {
                // Remove duplicated feed item which might be caused by updating (the same link but has two different pubDate).
                var items = queryRes.Results.GroupBy(i => i.PermentLink).Select(i => i.First()).ToList();
                if (items.Count > MAX_RETURN_COUNT)
                {
                    feed.NextRowKey = items[MAX_RETURN_COUNT].RowKey;
                    items.RemoveRange(MAX_RETURN_COUNT, items.Count - MAX_RETURN_COUNT);
                }
                else
                {
                    feed.NextRowKey = queryRes.ContinuationToken?.NextRowKey;
                }
                feed.Items = items.Select(i => i.CopyTo(new FeedItem())).ToList();
            }
            else
            {
                // We haven't refreshed this feed on the server (new added?), get it directly form rss.
                feed = await RefreshFeedAsync(string.IsNullOrWhiteSpace(feed.OriginalUri) ? feed.Uri : feed.OriginalUri);
            }

            // Mark readed or not.
            if (userFeedEntity != null && userFeedEntity.LastReadedTime != null)
            {
                foreach (var feedItem in feed.Items)
                {
                    if (feedItem.PubDate <= userFeedEntity.LastReadedTime)
                    {
                        feedItem.IsReaded = true;
                    }
                }
            }

            // Mark stared or not.
            if (userBlob != null)
            {
                var userEntity = await userBlob.GetAsync<UserEntity>();
                if (!string.IsNullOrWhiteSpace(userEntity.StaredHashs))
                {
                    var staredHashs = JsonConvert.DeserializeObject<SortedSet<string>>(userEntity.StaredHashs);
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
            }

            // All done.
            return feed;
        }

        public async Task RefreshFeedAsync(string feedOriginalUri, CloudTable feedTable, CloudTable feedItemTable)
        {
            await SaveFeedAsync(await RefreshFeedAsync(feedOriginalUri), feedTable, feedItemTable);
        }

        public async Task SubscribeFeedAsync(string feedOriginalUri, string customName, string customGroup, string userUuid, CloudTable usersFeedsTable, CloudTable feedTable)
        {
            var feedUri = feedOriginalUri.Trim().ToLower();
            var feedUriHash = Utils.Sha256(feedUri);

            // Do we have this feed already?
            Feed feed = null;
            var res = await feedTable.ExecuteAsync(TableOperation.Retrieve<FeedInfoEntity>(partitionKey: "feed_info", rowkey: feedUriHash));
            if (res == null || res.Result == null)
            {
                // Get information of this feed.
                feed = await RefreshFeedAsync(feedOriginalUri, noItems: true);

                // Save to feed table.
                try
                {
                    await feedTable.ExecuteAsync(TableOperation.Insert(new FeedInfoEntity(partitionKey: "feed_info", rowKey: feedUriHash, feed)));
                }
                catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.Conflict)
                {
                    // Save to ignore.
                }
            }
            else
            {
                feed = ((FeedInfoEntity)res.Result).CopyTo(new Feed());
            }

            // Use user customized name.
            if (!string.IsNullOrWhiteSpace(customName))
            {
                feed.Name = customName;
            }

            // User user customized group.
            feed.Group = customGroup;

            // Save to usersfeeds table.
            await usersFeedsTable.ExecuteAsync(TableOperation.InsertOrReplace(new UserFeedEntity(partitionKey: userUuid, rowKey: feedUriHash, feed)));
        }

        public async Task UnsubscribeFeedAsync(string feedUri, string userUuid, CloudTable usersFeedsTable)
        {
            // Get from usersFeedsTable table
            try
            {
                var res = await usersFeedsTable.ExecuteAsync(TableOperation.Retrieve<UserFeedEntity>(partitionKey: userUuid, rowkey: Utils.Sha256(feedUri)));
                if (res == null || res.Result == null)
                {
                    return;
                }

                await usersFeedsTable.ExecuteAsync(TableOperation.Delete((UserFeedEntity)res.Result));
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                // Save to ignore.
            }
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

        public async Task<List<FeedItem>> GetFeedItemsByCategory(FeedCategory category, string nextRowKey, Microsoft.Azure.Cosmos.Table.CloudTable latestFeedItemsTable)
        {
            // Generate filter.
            var partitionKey = category == FeedCategory.Recommended ? "Default" : category.ToString();
            Microsoft.Azure.Cosmos.Table.TableContinuationToken token = null;
            if (!string.IsNullOrWhiteSpace(nextRowKey))
            {
                token = new Microsoft.Azure.Cosmos.Table.TableContinuationToken()
                {
                    NextPartitionKey = partitionKey,
                    NextRowKey = nextRowKey
                };
            }
            var queryRes = await latestFeedItemsTable.ExecuteQuerySegmentedAsync(new Microsoft.Azure.Cosmos.Table.TableQuery<Backend.Share.Entities.FeedItemExEntity>()
            {
                TakeCount = MAX_RETURN_COUNT * 2,
                FilterString = Microsoft.Azure.Cosmos.Table.TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey)
            }, token);

            List<FeedItem> feedItems;
            if (queryRes != null && queryRes.Results != null && queryRes.Results.Count > 0)
            {
                // Remove duplicated feed item which might be caused by updating (the same link but has two different pubDate).
                var items = queryRes.Results.GroupBy(i => i.PermentLink).Select(i => i.First()).ToList();
                if (items.Count > MAX_RETURN_COUNT)
                {
                    nextRowKey = items[MAX_RETURN_COUNT].RowKey;
                    items.RemoveRange(MAX_RETURN_COUNT, items.Count - MAX_RETURN_COUNT);
                }
                else
                {
                    nextRowKey = queryRes.ContinuationToken?.NextRowKey;
                }
                feedItems = items.Select(i => i.CopyTo(new FeedItem())).ToList();
                feedItems.Last().NextRowKey = nextRowKey;
            }
            else
            {
                feedItems = new List<FeedItem>();
            }
            return feedItems;
        }

        public async Task UpdateFeedAsync(string feedUri, string newFeedName, string newFeedGroup, string userUuid, CloudTable usersFeedsTable)
        {
            // Get the original feed.
            var feedUriHash = Utils.Sha256(feedUri);
            var res = await usersFeedsTable.ExecuteAsync(TableOperation.Retrieve<UserFeedEntity>(userUuid, feedUriHash));
            if (res?.Result == null)
            {
                throw new ExternalErrorExceptionNotFound();
            }

            var userFeedEntity = (UserFeedEntity)res.Result;
            if (!string.IsNullOrWhiteSpace(newFeedName))
            {
                userFeedEntity.Name = newFeedName;
            }

            if (!string.IsNullOrWhiteSpace(newFeedGroup))
            {
                userFeedEntity.Group = newFeedGroup;
            }

            // Update
            await usersFeedsTable.ExecuteAsync(TableOperation.Replace(userFeedEntity));
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
    }
}
