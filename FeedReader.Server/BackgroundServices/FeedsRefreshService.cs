using FeedReader.Backend.Share;
using FeedReader.WebApi.AdminFunctions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FeedReader.Server.BackgroundServices
{
    public class FeedsRefreshService : IHostedService, IDisposable
    {
        private readonly ILogger<FeedsRefreshService> _logger;
        private CancellationTokenSource _cancelTokenSource;
        private Task _task;

        public FeedsRefreshService(ILogger<FeedsRefreshService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background.FeedsRefreshService is running.");

            _cancelTokenSource = new CancellationTokenSource();

            _task = Task.Run(() => RefreshFeeds(_cancelTokenSource.Token));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _cancelTokenSource?.Cancel();
            return _task != null ? _task : Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        private async void RefreshFeeds(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var startTime = DateTime.Now;
                try
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
                            foreach(var feedUri in items)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    break;
                                }

                                try
                                {
                                    await UpdateFeedFunc.UpdateFeed(feedUri, feedsTable, feedItemsTable, latestFeedItemsTable, _logger);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Background.FeedsRefreshService update {feedUri} throws exception: {ex}");
                                }
                            }
                        }
                        token = queryResult?.ContinuationToken;
                    } while (token != null && !cancellationToken.IsCancellationRequested);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Background.FeedsRefreshService throws exception: {ex}");
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    // Interval at least 10 minutes.
                    var endTime = DateTime.Now;
                    if (endTime - startTime < TimeSpan.FromMinutes(10))
                    {
                        Task.Delay(TimeSpan.FromMinutes(10) - (endTime - startTime), cancellationToken).Wait(cancellationToken);
                    }
                }
            }
        }
    }
}
