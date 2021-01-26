using FeedReader.Protos;
using FeedReader.WebApi.Extensions;
using FeedReader.WebApi.Processors;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
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
    }
}
