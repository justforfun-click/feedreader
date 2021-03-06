using FeedReader.Protos;
using FeedReader.ServerCore.Services;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FeedReader.WebServer.APIs
{
    public class GrpcApi : FeedReaderServerApi.FeedReaderServerApiBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IFeedService _feedService;

        public GrpcApi(IServiceProvider sp)
        {
            _authService = sp.GetService<IAuthService>();
            _userService = sp.GetService<IUserService>();
            _feedService = sp.GetService<IFeedService>();
        }

        public override async Task<UserInfo> Login(LoginRequest request, ServerCallContext context)
        {
            try
            {
                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                user = await _userService.Login(user);
                var res = new UserInfo
                {
                    Token = user.Token,
                    Uuid = user.Id
                };
                if (user.Feeds != null)
                {
                    res.Feeds.AddRange(user.Feeds.Select(f => GetFeedInfo(f)));
                }
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
                await _userService.MarkItemsAsReaded(user, request.FeedUri, request.Timestamp.ToDateTime().ToUniversalTime());
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
                var feed = await _userService.GetFeedItemsAsync(user, request.FeedUri, request.Page);
                var response = new RefreshFeedResponse()
                {
                    FeedInfo = GetFeedInfo(feed),
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
            var items = await _feedService.GetCategoryFeedItems(GetModesFeedCategory(request.Category), request.Page);
            var response = new GetFeedsByCategoryResponse();
            if (items?.Count > 0)
            {
                response.FeedItems.AddRange(items.Select(f => GetFeedItemMessageWithFeedInfo(f)));
            }
            return response;
        }

        public override async Task<GetStaredFeedItemsResponse> GetStaredFeedItems(GetStaredFeedItemsRequest request, ServerCallContext context)
        {
            try
            {
                var user = await _authService.AuthenticateTokenAsync(context.RequestHeaders.Get("authentication")?.Value);
                var items = await _userService.GetStaredFeedItemsAsync(user, request.Page);
                var response = new GetStaredFeedItemsResponse();
                if (items.Count > 0)
                {
                    response.FeedItems.AddRange(items.Select(f => GetFeedItemMessageWithFeedInfo(f)));
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
                await _userService.StarFeedItemAsync(user, request.FeedItem);
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
                await _userService.UnstarFeedItemAsync(user, request.FeedItemUri, request.FeedItemPubDate.ToDateTime().ToUniversalTime());
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
                var feed = await _userService.SubscribeFeedAsync(user, request.OriginalUri, request.Group);
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
                await _userService.UnsubscribeFeedAsync(user, request.FeedUri);
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
                await _userService.UpdateFeedAsync(user, request.FeedUri, request.FeedGroup);
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

        private FeedInfo GetFeedInfo(ServerCore.Models.UserFeed userFeed)
        {
            var feed = userFeed.Feed;
            return new FeedInfo
            {
                Description = feed.Description ?? string.Empty,
                Group = userFeed.Group ?? string.Empty,
                IconUri = feed.IconUri ?? string.Empty,
                Name = feed.Name ?? string.Empty,
                OriginalUri = feed.Uri,
                Uri = feed.Uri,
                WebsiteLink = feed.WebSiteUri ?? string.Empty
            };
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

        private FeedItemMessage GetFeedItemMessage(ServerCore.Models.FeedItem f)
        {
            return new FeedItemMessage
            {
                Content = f.Content ?? string.Empty,
                PermentLink = f.Uri,
                PubDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(f.PublishTimeInUtc),
                Summary = f.Summary ?? string.Empty,
                Title = f.Title ?? string.Empty,
                TopicPictureUri = f.TopicPictureUri ?? string.Empty
            };
        }

        private FeedItemMessageWithFeedInfo GetFeedItemMessageWithFeedInfo(ServerCore.Models.FeedItem f)
        {
            return new FeedItemMessageWithFeedInfo
            {
                FeedItem = GetFeedItemMessage(f),
                FeedUri = f.Feed.Uri,
                FeedIconUri = f.Feed.IconUri ?? string.Empty,
                FeedName = f.Feed.Name ?? string.Empty
            };
        }

        private ServerCore.Models.FeedCategory GetModesFeedCategory(Protos.FeedCategory category)
        {
            switch (category)
            {
                case FeedCategory.Default:
                    return ServerCore.Models.FeedCategory.Default;

                case FeedCategory.Art:
                    return ServerCore.Models.FeedCategory.Art;

                case FeedCategory.Business:
                    return ServerCore.Models.FeedCategory.Business;

                case FeedCategory.News:
                    return ServerCore.Models.FeedCategory.News;

                case FeedCategory.Sport:
                    return ServerCore.Models.FeedCategory.Sport;

                case FeedCategory.Technology:
                    return ServerCore.Models.FeedCategory.Technology;

                case FeedCategory.Kids:
                    return ServerCore.Models.FeedCategory.Kids;

                default:
                    throw new ArgumentException($"Bad category: {category}");
            }
        }
    }
}
