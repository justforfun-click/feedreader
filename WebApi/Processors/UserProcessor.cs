using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Extensions;
using JWT.Algorithms;
using JWT.Builder;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeedReader.Share;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Queue;
using FeedReader.Backend.Share;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos.Table;
using Azure.Storage.Queues;

namespace FeedReader.WebApi.Processors
{
    public class UserProcessor : Processor
    {
        const int MAX_RETURN_COUNT = 50;

        public UserProcessor(ILogger logger = null)
            : base(logger)
        {
        }

        public async Task<User> LoginAsync(UserEntity user, BlobContainerClient userContainer, CloudTable uuidIndexTable, CloudTable usersFeedsTable)
        {
            // Get feedreader uuid?
            var uuid = await GetFeedReaderUuid(user, userContainer, uuidIndexTable);

            // Login, reget the user data.
            var userEntity = await userContainer.GetBlobClient(uuid).GetAsync<UserEntity>();
            if (userEntity == null)
            {
                throw new ExternalErrorExceptionUnauthentication();
            }

            // Generate our jwt token.
            var now = DateTimeOffset.UtcNow;
            var token = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Environment.GetEnvironmentVariable(Consts.ENV_KEY_JWT_SECRET))
                .AddClaim("iss", Consts.FEEDREADER_ISS)
                .AddClaim("aud", Consts.FEEDREADER_AUD)
                .AddClaim("uuid", userEntity.Uuid)
                .AddClaim("iat", now.ToUnixTimeSeconds())
                .AddClaim("exp", now.AddDays(7).ToUnixTimeSeconds())
                .Encode();

            // Get users feeds table.
            var res = await usersFeedsTable.ExecuteQuerySegmentedAsync(new TableQuery<UserFeedEntity>()
            {
                FilterString = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userEntity.Uuid)
            }, null);

            // Return user info
            return new User
            {
                Token = token,
                Uuid = userEntity.Uuid,
                Feeds = (res != null && res.Results != null) ? res.Results.Select(f => f.CopyTo(new Feed())).ToList() : new List<Feed>(),
            };
        }

        public async Task StarFeedItemAsync(FeedItem feedItem, BlobClient userBlob, Microsoft.Azure.Cosmos.Table.CloudTable userFeedItemStartsTable)
        {
            // Get the hash set.
            SortedSet<string> staredHashs;
            var userEntity = await userBlob.GetAsync<UserEntity>();
            if (!string.IsNullOrWhiteSpace(userEntity.StaredHashs))
            {
                staredHashs = JsonConvert.DeserializeObject<SortedSet<string>>(userEntity.StaredHashs);
            }
            else
            {
                staredHashs = new SortedSet<string>();
            }

            // Insert to the stared-feed-items table.
            await userFeedItemStartsTable.ExecuteAsync(Microsoft.Azure.Cosmos.Table.TableOperation.InsertOrReplace(new Backend.Share.Entities.FeedItemExEntity()
            {
                PartitionKey = userEntity.Uuid,
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
            var linkMd5 = feedItem.PermentLink.Md5();
            if (!staredHashs.Contains(linkMd5) && staredHashs.Add(linkMd5))
            {
                userEntity.StaredHashs = JsonConvert.SerializeObject(staredHashs);
                await userBlob.SaveAsync(userEntity);
            }
        }

        public async Task UnstarFeedItemAsync(string feedItemPermentLink, DateTime pubDate, BlobClient userBlob, Microsoft.Azure.Cosmos.Table.CloudTable userFeedItemStartsTable)
        {
            // Get the hash set.
            var userEntity = await userBlob.GetAsync<UserEntity>();

            // Remove from the star items table.
            var partitionKey = userEntity.Uuid;
            var rowKey = $"{string.Format("{0:D19}", DateTime.MaxValue.Ticks - pubDate.ToUniversalTime().Ticks)}-{feedItemPermentLink.Sha256()}";
            var res = await userFeedItemStartsTable.ExecuteAsync(Microsoft.Azure.Cosmos.Table.TableOperation.Retrieve<Backend.Share.Entities.FeedItemExEntity>(partitionKey: partitionKey, rowkey: rowKey));
            if (res?.Result != null)
            {
                await userFeedItemStartsTable.ExecuteAsync(Microsoft.Azure.Cosmos.Table.TableOperation.Delete((Backend.Share.Entities.FeedItemExEntity)res.Result));
            }

            // Remove from the hash set.
            var linkMd5 = feedItemPermentLink.Md5();
            if (!string.IsNullOrWhiteSpace(userEntity.StaredHashs))
            {
                var staredHashs = JsonConvert.DeserializeObject<SortedSet<string>>(userEntity.StaredHashs);
                if (staredHashs.Contains(linkMd5) && staredHashs.Remove(linkMd5))
                {
                    userEntity.StaredHashs = JsonConvert.SerializeObject(staredHashs);
                    await userBlob.SaveAsync(userEntity);
                }
            }
        }

        public async Task<List<FeedItem>> GetStaredFeedItemsAsync(string nextRowKey, string userUuid, Microsoft.Azure.Cosmos.Table.CloudTable userFeedItemStartsTable)
        {
            Microsoft.Azure.Cosmos.Table.TableContinuationToken token = null;
            if (!string.IsNullOrWhiteSpace(nextRowKey))
            {
                token = new Microsoft.Azure.Cosmos.Table.TableContinuationToken()
                {
                    NextPartitionKey = userUuid,
                    NextRowKey = nextRowKey
                };
            }

            var queryRes = await userFeedItemStartsTable.ExecuteQuerySegmentedAsync(new Microsoft.Azure.Cosmos.Table.TableQuery<Backend.Share.Entities.FeedItemExEntity>()
            {
                TakeCount = MAX_RETURN_COUNT * 2,
                FilterString = Microsoft.Azure.Cosmos.Table.TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, userUuid)
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

        public async Task MarkItemsAsReaded(string userUuid, string feedUri, DateTime lastReadedTime, CloudTable usersFeedsTable, CloudTable feedsTable, QueueClient feedRefreshJobs)
        {
            var feedUriHash = Utils.Sha256(feedUri);
            var res = await usersFeedsTable.ExecuteAsync(TableOperation.Retrieve<UserFeedEntity>(partitionKey: userUuid, rowkey: feedUriHash, new List<string>() { "ETag" }));
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

        private static async Task<string> GetFeedReaderUuid(UserEntity user, BlobContainerClient userContainer, CloudTable uuidIndexTable)
        {
            // If it is feedshub uuid, return directly.
            if (user.Uuid.StartsWith(Consts.FEEDREADER_UUID_PREFIX))
            {
                return user.Uuid;
            }

            // Not feedreader uuid, try to find from the related uuid index.
            var res = await uuidIndexTable.ExecuteAsync(TableOperation.Retrieve<RelatedUuidEntity>(partitionKey: user.Uuid, rowkey: user.Uuid));
            if (res?.Result != null)
            {
                return ((RelatedUuidEntity)res.Result).FeedReaderUuid;
            }

            // Not found, let's register it.
            var feedshubUuid = Consts.FEEDREADER_UUID_PREFIX + Guid.NewGuid().ToString("N").ToLower();
            await uuidIndexTable.ExecuteAsync(TableOperation.Insert(new RelatedUuidEntity()
            {
                PartitionKey = user.Uuid,
                RowKey = user.Uuid,
                ThirdPartyUUid = user.Uuid,
                FeedReaderUuid = feedshubUuid
            }));

            // Create user.
            var userEntity = new UserEntity()
            {
                PartitionKey = feedshubUuid,
                RowKey = feedshubUuid,
                Uuid = feedshubUuid,
                Email = user.Email,
                Name = user.Name,
                RegistrationTime = DateTime.Now,
                AvatarUrl = user.AvatarUrl
            };
            if (string.IsNullOrWhiteSpace(userEntity.AvatarUrl))
            {
                if (!string.IsNullOrWhiteSpace(userEntity.Email))
                {
                    userEntity.AvatarUrl = $"https://s.gravatar.com/avatar/{user.Email.Md5()}?s=256";
                }
                else
                {
                    userEntity.AvatarUrl = $"https://s.gravatar.com/avatar/?s=256";
                }
            }
            await userContainer.GetBlobClient(feedshubUuid).SaveAsync(userEntity);
            return feedshubUuid;
        }
    }
}
