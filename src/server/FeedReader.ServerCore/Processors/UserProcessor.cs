using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Extensions;
using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeedReader.Share;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using Azure.Storage.Queues;
using User = FeedReader.ServerCore.Models.User;
using Microsoft.EntityFrameworkCore;
using FeedReader.ServerCore.Datas;

namespace FeedReader.WebApi.Processors
{
    public class UserProcessor : Processor
    {
        const int MAX_RETURN_COUNT = 50;

        private IDbContextFactory<FeedReaderDbContext> _dbFactory;

        public UserProcessor(IDbContextFactory<FeedReaderDbContext> dbFactory, ILogger logger = null)
            : base(logger)
        {
            _dbFactory = dbFactory;
        }

        public async Task<Share.DataContracts.User> LoginAsync(User user, CloudTable usersFeedsTable)
        {
            var db = _dbFactory.CreateDbContext();

            // If it is feedreader user already (has id property), query user in db.
            if (!string.IsNullOrEmpty(user.Id))
            {
                user = await db.Users.FindAsync(user.Id);
                if (user == null)
                {
                    throw new UnauthorizedAccessException();
                }
            }
            else
            {
                // Not feedreader uuid, try to find from the related uuid index.
                var dbUser = await db.Users.FirstOrDefaultAsync(u => u.ThirdPartyId == user.ThirdPartyId);
                if (dbUser != null)
                {
                    user = dbUser;
                }
                else
                {
                    // Not found, let's register it.
                    user.Id = Guid.NewGuid().ToString("N").ToLower();
                    user.RegistrationTimeInUtc = DateTime.UtcNow;
                    db.Users.Add(user);
                }
            }

            // Update last active time.
            user.LastActiveTimeInUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Generate our jwt token.
            var now = DateTimeOffset.UtcNow;
            var token = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Environment.GetEnvironmentVariable(Consts.ENV_KEY_JWT_SECRET))
                .AddClaim("iss", Consts.FEEDREADER_ISS)
                .AddClaim("aud", Consts.FEEDREADER_AUD)
                .AddClaim("uid", user.Id)
                .AddClaim("iat", now.ToUnixTimeSeconds())
                .AddClaim("exp", now.AddDays(7).ToUnixTimeSeconds())
                .Encode();

            // Get users feeds table.
            var res = await usersFeedsTable.ExecuteQuerySegmentedAsync(new TableQuery<UserFeedEntity>()
            {
                FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Consts.FEEDREADER_UUID_PREFIX + user.Id)
            }, null);

            // Return user info
            return new Share.DataContracts.User
            {
                Token = token,
                Uuid = user.Id,
                Feeds = (res != null && res.Results != null) ? res.Results.Select(f => f.CopyTo(new Feed())).ToList() : new List<Feed>(),
            };
        }

        public async Task StarFeedItemAsync(FeedItem feedItem, User user, Microsoft.Azure.Cosmos.Table.CloudTable userFeedItemStartsTable)
        {
            // Insert to the stared-feed-items table.
            await userFeedItemStartsTable.ExecuteAsync(Microsoft.Azure.Cosmos.Table.TableOperation.InsertOrReplace(new Backend.Share.Entities.FeedItemExEntity()
            {
                PartitionKey = Consts.FEEDREADER_UUID_PREFIX + user.Id,
                RowKey = $"{string.Format("{0:D19}", DateTime.MaxValue.Ticks - feedItem.PubDate.ToUniversalTime().Ticks)}-{feedItem.PermentLink.Sha256()}",
                PermentLink = feedItem.PermentLink,
                Content = feedItem.Content,
                FeedIconUri = feedItem.FeedIconUri,
                FeedName = feedItem.FeedName,
                FeedUri = feedItem.FeedUri,
                PubDate = feedItem.PubDate,
                Summary = feedItem.Summary,
                Title = feedItem.Title,
                TopicPictureUri = feedItem.TopicPictureUri,
            }));

            // Update the hash set.
            var db = _dbFactory.CreateDbContext();
            var linkMd5 = feedItem.PermentLink.Md5();
            try
            {
                db.UserFavorites.Add(new ServerCore.Models.UserFavorites
                {
                    UserId = user.Id,
                    FavoriteItemIdHash = linkMd5
                });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogError($"Add item to UserFavorites table failed: {ex.Message}");
            }
        }

        public async Task UnstarFeedItemAsync(string feedItemPermentLink, DateTime pubDate, User user, Microsoft.Azure.Cosmos.Table.CloudTable userFeedItemStartsTable)
        {
            // Remove from the star items table.
            var partitionKey = Consts.FEEDREADER_UUID_PREFIX + user.Id;
            var rowKey = $"{string.Format("{0:D19}", DateTime.MaxValue.Ticks - pubDate.ToUniversalTime().Ticks)}-{feedItemPermentLink.Sha256()}";
            var res = await userFeedItemStartsTable.ExecuteAsync(Microsoft.Azure.Cosmos.Table.TableOperation.Retrieve<Backend.Share.Entities.FeedItemExEntity>(partitionKey: partitionKey, rowkey: rowKey));
            if (res?.Result != null)
            {
                await userFeedItemStartsTable.ExecuteAsync(Microsoft.Azure.Cosmos.Table.TableOperation.Delete((Backend.Share.Entities.FeedItemExEntity)res.Result));
            }

            // Remove from the hash set.
            var linkMd5 = feedItemPermentLink.Md5();
            var db = _dbFactory.CreateDbContext();
            try
            {
                var favorite = new FeedReader.ServerCore.Models.UserFavorites
                {
                    UserId = user.Id,
                    FavoriteItemIdHash = linkMd5
                };
                db.UserFavorites.Attach(favorite);
                db.UserFavorites.Remove(favorite);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                LogError($"Remove item from UserFavorites table failed: {ex.Message}");
            }
        }

        public async Task<List<FeedItem>> GetStaredFeedItemsAsync(string nextRowKey, User user, Microsoft.Azure.Cosmos.Table.CloudTable userFeedItemStartsTable)
        {
            Microsoft.Azure.Cosmos.Table.TableContinuationToken token = null;
            if (!string.IsNullOrWhiteSpace(nextRowKey))
            {
                token = new Microsoft.Azure.Cosmos.Table.TableContinuationToken()
                {
                    NextPartitionKey = Consts.FEEDREADER_UUID_PREFIX + user.Id,
                    NextRowKey = nextRowKey
                };
            }

            var queryRes = await userFeedItemStartsTable.ExecuteQuerySegmentedAsync(new Microsoft.Azure.Cosmos.Table.TableQuery<Backend.Share.Entities.FeedItemExEntity>()
            {
                TakeCount = MAX_RETURN_COUNT * 2,
                FilterString = Microsoft.Azure.Cosmos.Table.TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, Consts.FEEDREADER_UUID_PREFIX + user.Id)
            }, token);

            List<FeedItem> feedItems;
            if (queryRes != null && queryRes.Results != null && queryRes.Results.Count > 0)
            {
                if (queryRes.Results.Count > MAX_RETURN_COUNT)
                {
                    nextRowKey = queryRes.Results[MAX_RETURN_COUNT].RowKey;
                    queryRes.Results.RemoveRange(MAX_RETURN_COUNT, queryRes.Results.Count - MAX_RETURN_COUNT);
                }
                else
                {
                    nextRowKey = queryRes.ContinuationToken?.NextRowKey;
                }
                feedItems = queryRes.Results.Select(i => i.CopyTo(new FeedItem())).ToList();
                feedItems.Last().NextRowKey = nextRowKey;
            }
            else
            {
                feedItems = new List<FeedItem>();
            }
            return feedItems;
        }

        public async Task MarkItemsAsReaded(User user, string feedUri, DateTime lastReadedTime, CloudTable usersFeedsTable, CloudTable feedsTable, QueueClient feedRefreshJobs)
        {
            var feedUriHash = Utils.Sha256(feedUri);
            var res = await usersFeedsTable.ExecuteAsync(TableOperation.Retrieve<UserFeedEntity>(partitionKey: Consts.FEEDREADER_UUID_PREFIX + user.Id, rowkey: feedUriHash, new List<string>() { "ETag" }));
            if (res?.Result == null)
            {
                throw new ExternalErrorExcepiton("'feedUri' is not found.");
            }
            var userFeed = (UserFeedEntity)res.Result;

            // Get the latest feed from the feed info table.
            var feedInfoRes = await feedsTable.ExecuteAsync(TableOperation.Retrieve<FeedInfoEntity>(partitionKey: "feed_info", rowkey: feedUriHash));
            if (feedInfoRes == null || feedInfoRes.Result == null)
            {
                LogError($"{feedUri} can't be found in feed info table, but exists in user feed table.");

                // Send a message to feed refresh jobs queue.
                _ = feedRefreshJobs.SendMessageAsync(userFeed.OriginalUri.Base64());
            }
            else
            {
                // Update with latest feed info.
                var feedInfo = (FeedInfoEntity)feedInfoRes.Result;
                userFeed.Description = feedInfo.Description;
                userFeed.IconUri = feedInfo.IconUri;
                userFeed.WebsiteLink = feedInfo.WebsiteLink;
            }

            userFeed.LastReadedTime = lastReadedTime;
            await usersFeedsTable.ExecuteAsync(TableOperation.Merge(userFeed));
        }
    }
}
