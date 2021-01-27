using FeedReader.Protos;
using FeedReader.WebApi.Extensions;
using FeedReader.WebApi.Processors;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FeedReader.Server.Services
{
    public class ApiService : FeedReaderServerApi.FeedReaderServerApiBase
    {
        private readonly AuthService _authService;

        public ApiService(IServiceProvider sp)
        {
            _authService = sp.GetService<AuthService>();
        }

        public override async Task<Empty> MarkFeedAsReadedFromTimestamp(MarkFeedAsReadedFromTimestampRequest request, ServerCallContext context)
        {
            try
            {
                var user = _authService.AuthenticateToken(context.RequestHeaders.Get("authentication")?.Value);
                var userFeedsTable = AzureStorage.GetUsersFeedsTable();
                var feedsTable = Backend.Share.AzureStorage.GetFeedsTable();
                var feedRefresJobsQueue = Backend.Share.AzureStorage.GetFeedRefreshJobsQueue();
                await new UserProcessor(null).MarkItemsAsReaded(user.Uuid, request.FeedUri, request.Timestamp.ToDateTime(), userFeedsTable, feedsTable, feedRefresJobsQueue);
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
                var user = userToken == null ? null : _authService.AuthenticateToken(userToken);
                var userBlob = user == null ? null : AzureStorage.GetUserBlob(user.Uuid);
                var userFeedsTable = AzureStorage.GetUsersFeedsTable();
                var feedsTable = Backend.Share.AzureStorage.GetFeedsTable();
                var feedItemsTable = Backend.Share.AzureStorage.GetFeedItemsTable();
                var feed = await new FeedProcessor().GetFeedItemsAsync(request.FeedUri, request.NextRowKey, userBlob, userFeedsTable, feedsTable, feedItemsTable);
                var response = new RefreshFeedResponse()
                {
                    FeedInfo = new FeedInfo
                    {
                        Description = feed.Description ?? string.Empty,
                        Group = feed.Group ?? string.Empty,
                        IconUri = feed.IconUri ?? string.Empty,
                        Name = feed.Name ?? string.Empty,
                        OriginalUri = feed.OriginalUri ?? feed.Uri,
                        Uri = feed.Uri,
                        WebsiteLink = feed.WebsiteLink ?? string.Empty
                    }
                };
                response.FeedItems.AddRange(feed.Items.Select(f => new FeedItemMessage
                {
                    Content = f.Content ?? string.Empty,
                    IsReaded = f.IsReaded,
                    IsStarted = f.IsStared,
                    PermentLink = f.PermentLink ?? string.Empty,
                    PubDate = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(f.PubDate.ToUniversalTime()),
                    Summary = f.Summary ?? string.Empty,
                    Title = f.Title ?? string.Empty,
                    TopicPictureUri = f.TopicPictureUri ?? string.Empty
                }));
                return response;
            }
            catch (UnauthorizedAccessException)
            {
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Unauthenticated"));
            }
        }
    }
}
