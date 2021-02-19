using FeedReader.Protos;
using FeedReader.ServerCore;
using FeedReader.ServerCore.Datas;
using FeedReader.WebApi;
using FeedReader.WebApi.Processors;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FeedReader.Server.Services
{
    public class ApiService : FeedReaderServerApi.FeedReaderServerApiBase
    {
        private readonly AuthService _authService;
        private readonly IDbContextFactory<FeedReaderDbContext> _dbContext;

        public ApiService(IServiceProvider sp)
        {
            _authService = sp.GetService<AuthService>();
            _dbContext = sp.GetService<IDbContextFactory<FeedReaderDbContext>>();
        }

        public override async Task<UserInfo> Login(LoginRequest request, ServerCallContext context)
        {
            try
            {
                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                var processor = new UserProcessor(_dbContext);
                var usersFeedsTable = AzureStorage.GetUsersFeedsTable();
                var userEntity = await processor.LoginAsync(user, usersFeedsTable);
                var res = new UserInfo
                {
                    Token = userEntity.Token,
                    Uuid = userEntity.Uuid
                };
                res.Feeds.AddRange(userEntity.Feeds.Select(f => GetFeedInfo(f)));
                return res;
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }

        public override async Task<Empty> MarkFeedAsReadedFromTimestamp(MarkFeedAsReadedFromTimestampRequest request, ServerCallContext context)
        {
            try
            {
                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                var userFeedsTable = AzureStorage.GetUsersFeedsTable();
                var feedRefresJobsQueue = AzureStorage.GetFeedRefreshJobsQueue();
                await new UserProcessor(_dbContext).MarkItemsAsReaded(user, request.FeedUri, request.Timestamp.ToDateTime(), userFeedsTable, feedRefresJobsQueue);
                return new Empty();
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }

        public override async Task<RefreshFeedResponse> RefreshFeed(RefreshFeedRequest request, ServerCallContext context)
        {
            try
            {
                var userToken = context.RequestHeaders.Get("authentication")?.Value;
                var user = userToken == null ? null : await _authService.AuthenticateTokenAsync(userToken);
                var userFeedsTable = AzureStorage.GetUsersFeedsTable();
                var feedItemsTable = AzureStorage.GetFeedItemsTable();
                var feed = await new FeedProcessor(_dbContext).GetFeedItemsAsync(request.FeedUri, request.NextRowKey, user, userFeedsTable, feedItemsTable);
                var response = new RefreshFeedResponse()
                {
                    FeedInfo = GetFeedInfo(feed),
                    NextRowKey = feed.NextRowKey ?? string.Empty,
                };
                response.FeedItems.AddRange(feed.Items.Select(f => GetFeedItemMessage(f)));
                return response;
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }

        public override async Task<GetFeedsByCategoryResponse> GetFeedsByCategory(GetFeedsByCategoryRequest request, ServerCallContext context)
        {
            var items = await new FeedProcessor(_dbContext).GetFeedItemsByCategory(GetDataContractsFeedCategory(request.Category), request.NextRowKey, AzureStorage.GetLatestFeedItemsTable());
            var response = new GetFeedsByCategoryResponse();
            if (items.Count > 0)
            {
                response.FeedItems.AddRange(items.Select(f => GetFeedItemMessageWithFeedInfo(f)));
                response.NextRowKey = items.Last().NextRowKey ?? string.Empty;
            }
            return response;
        }

        public override async Task<GetStaredFeedItemsResponse> GetStaredFeedItems(GetStaredFeedItemsRequest request, ServerCallContext context)
        {
            try
            {
                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                var items = await new UserProcessor(_dbContext).GetStaredFeedItemsAsync(request.NextRowKey, user, AzureStorage.GetUserStaredFeedItemsTable());
                var response = new GetStaredFeedItemsResponse();
                if (items.Count > 0)
                {
                    response.FeedItems.AddRange(items.Select(f => GetFeedItemMessageWithFeedInfo(f)));
                    response.NextRowKey = items.Last().NextRowKey ?? string.Empty;
                }
                return response;
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }

        public override async Task<Empty> StarFeedItem(StarFeedItemRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FeedItem.FeedItem.PermentLink))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "'PermentLink' of feed item is missing."));
                }

                if (string.IsNullOrEmpty(request.FeedItem.FeedUri))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "'FeedUri' of feed item is missing."));
                }

                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                await new UserProcessor(_dbContext).StarFeedItemAsync(GetDataContractFeedItem(request.FeedItem), user, AzureStorage.GetUserStaredFeedItemsTable());
                return new Empty();
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }

        public override async Task<Empty> UnstarFeedItem(UnstarFeedItemRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FeedItemUri))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "'FeedItemUri' is missing."));
                }

                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                await new UserProcessor(_dbContext).UnstarFeedItemAsync(request.FeedItemUri, request.FeedItemPubDate.ToDateTime(), user, AzureStorage.GetUserStaredFeedItemsTable());
                return new Empty();
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }

        public override async Task<SubscribeFeedResponse> SubscribeFeed(SubscribeFeedRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(request.OriginalUri))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "'Original' is missing."));
                }

                request.OriginalUri = request.OriginalUri.Trim();
                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                var usersFeedsTable = AzureStorage.GetUsersFeedsTable();
                var feed = await new FeedProcessor(_dbContext).SubscribeFeedAsync(request.OriginalUri, request.Name, request.Group, Consts.FEEDREADER_UUID_PREFIX + user.Id, usersFeedsTable);
                return new SubscribeFeedResponse
                {
                    Feed = GetFeedInfo(feed)
                };
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }

        public override async Task<Empty> UnsubscribeFeed(UnsubscribeFeedRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FeedUri))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "'FeedUri' is missing."));
                }

                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                await new FeedProcessor(_dbContext).UnsubscribeFeedAsync(request.FeedUri, Consts.FEEDREADER_UUID_PREFIX + user.Id, AzureStorage.GetUsersFeedsTable());
                return new Empty();
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }

        public override async Task<Empty> UpdateFeed(UpdateFeedRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(request.FeedUri))
                {
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "'FeedUri' is missing."));
                }

                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                await new FeedProcessor(_dbContext).UpdateFeedAsync(request.FeedUri, request.FeedName, request.FeedGroup, Consts.FEEDREADER_UUID_PREFIX + user.Id, AzureStorage.GetUsersFeedsTable());
                return new Empty();
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }

        private Share.DataContracts.FeedCategory GetDataContractsFeedCategory(FeedCategory category)
        {
            switch (category)
            {
                default:
                case FeedCategory.Default:
                    return Share.DataContracts.FeedCategory.Recommended;

                case FeedCategory.Art:
                    return Share.DataContracts.FeedCategory.Art;

                case FeedCategory.Business:
                    return Share.DataContracts.FeedCategory.Business;

                case FeedCategory.News:
                    return Share.DataContracts.FeedCategory.News;

                case FeedCategory.Sport:
                    return Share.DataContracts.FeedCategory.Sports;

                case FeedCategory.Technology:
                    return Share.DataContracts.FeedCategory.Technology;

                case FeedCategory.Kids:
                    return Share.DataContracts.FeedCategory.Kids;
            }
        }

        private FeedInfo GetFeedInfo(Share.DataContracts.Feed feed)
        {
            return new FeedInfo
            {
                Description = feed.Description ?? string.Empty,
                Group = feed.Group ?? string.Empty,
                IconUri = feed.IconUri ?? string.Empty,
                Name = feed.Name ?? string.Empty,
                OriginalUri = feed.OriginalUri ?? feed.Uri,
                Uri = feed.Uri,
                WebsiteLink = feed.WebsiteLink ?? string.Empty
            };
        }

        private Share.DataContracts.FeedItem GetDataContractFeedItem(FeedItemMessage f)
        {
            return new Share.DataContracts.FeedItem
            {
                Content = f.Content,
                IsReaded = f.IsReaded,
                IsStared = f.IsReaded,
                PermentLink = f.PermentLink,
                PubDate = f.PubDate.ToDateTime(),
                Summary = f.Summary,
                Title = f.Title,
                TopicPictureUri = f.TopicPictureUri
            };
        }

        private Share.DataContracts.FeedItem GetDataContractFeedItem(FeedItemMessageWithFeedInfo f)
        {
            var feedItem = GetDataContractFeedItem(f.FeedItem);
            feedItem.FeedIconUri = f.FeedIconUri;
            feedItem.FeedName = f.FeedName;
            feedItem.FeedUri = f.FeedUri;
            return feedItem;
        }

        private FeedItemMessage GetFeedItemMessage(Share.DataContracts.FeedItem f)
        {
            return new FeedItemMessage
            {
                Content = f.Content ?? string.Empty,
                IsReaded = f.IsReaded,
                IsStared = f.IsStared,
                PermentLink = f.PermentLink ?? string.Empty,
                PubDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(f.PubDate.ToUniversalTime()),
                Summary = f.Summary ?? string.Empty,
                Title = f.Title ?? string.Empty,
                TopicPictureUri = f.TopicPictureUri ?? string.Empty
            };
        }

        private FeedItemMessageWithFeedInfo GetFeedItemMessageWithFeedInfo(Share.DataContracts.FeedItem f)
        {
            return new FeedItemMessageWithFeedInfo
            {
                FeedItem = GetFeedItemMessage(f),
                FeedUri = f.FeedUri,
                FeedIconUri = f.FeedIconUri ?? string.Empty,
                FeedName = f.FeedName ?? string.Empty
            };
        }
    }
}
