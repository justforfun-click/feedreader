using FeedReader.Backend.Share;
using FeedReader.WebApi.AdminFunctions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FeedReader.TaskServer.Tasks
{
    class FeedRefreshTask : TaskBase
    {
        public FeedRefreshTask(ILogger<FeedRefreshTask> logger)
            : base("FeedRefreshTask", TimeSpan.FromMinutes(10), logger)
        {
        }

        protected override async Task DoTaskOnce(CancellationToken cancellationToken)
        {
            TableContinuationToken token = null;
            var feedsTable = AzureStorage.GetFeedsTable();
            var feedItemsTable = AzureStorage.GetFeedItemsTable();
            var latestFeedItemsTable = AzureStorage.GetLatestFeedItemsTable();
            var results = new List<string>();
            do
            {
                var queryResult = await feedsTable.ExecuteQuerySegmentedAsync(new TableQuery()
                {
                    SelectColumns = new List<string>() { "Uri" }
                }, token);

                var items = queryResult?.Results?.Select(r => r["Uri"].StringValue).ToArray();
                if (items != null)
                {
                    foreach (var feedUri in items)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        try
                        {
                            await UpdateFeedFunc.UpdateFeed(feedUri, feedsTable, feedItemsTable, latestFeedItemsTable, Logger);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Background.FeedsRefreshService update {feedUri} throws exception: {ex}");
                        }
                    }
                }
                token = queryResult?.ContinuationToken;
            } while (token != null && !cancellationToken.IsCancellationRequested);
        }
    }
}
