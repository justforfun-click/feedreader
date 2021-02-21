using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using System.Net.Http;
using FeedReader.Backend.Share.FeedParsers;
using FeedReader.WebApi.Processors;
using FeedReader.Backend.Share.Entities;
using System.Net.Http.Headers;
using FeedReader.WebApi.Extensions;
using Microsoft.EntityFrameworkCore;
using FeedReader.ServerCore.Datas;
using FeedReader.ServerCore;
using System.Threading;

namespace FeedReader.WebApi.AdminFunctions
{
    public static class UpdateFeedFunc
    {
        public static async Task UpdateFeeds(IDbContextFactory<FeedReaderDbContext> dbFactory, CancellationToken cancellationToken, ILogger logger)
        {
            var db = dbFactory.CreateDbContext();
            var feedItemsTable = AzureStorage.GetFeedItemsTable();
            var latestFeedItemsTable = AzureStorage.GetLatestFeedItemsTable();
            int count = 0;
            foreach (var feed in await db.Feeds.ToListAsync())
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await UpdateFeed(dbFactory, feed, feedItemsTable, latestFeedItemsTable, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Background.FeedsRefreshService update {feed.Uri} throws exception: {ex}");
                }

                if (++count == 50)
                {
                    await db.SaveChangesAsync();
                }
            }
            await db.SaveChangesAsync();
        }

        public static async Task UpdateFeed(IDbContextFactory<FeedReaderDbContext> dbFactory, ServerCore.Models.Feed feed, CloudTable feedItemsTable, CloudTable latestFeedItemsTable, ILogger log, HttpClient httpClient = null)
        {
            log.LogInformation($"UpdateFeed: {feed.Uri}");

            // Get original uri.
            var feedUriHash = feed.Id;
            var feedOriginalUri = feed.Uri;

            // Get feed content.
            if (httpClient == null)
            {
                var handler = new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.All, AllowAutoRedirect = true };
                httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "FeedReader");
                httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            }

            var feedContent = await httpClient.GetStringAsync(feedOriginalUri);

            // Create parser.
            var parser = FeedParser.Create(feedContent);

            // Parse feed info.
            var feedInfo = parser.ParseFeedInfo();

            // Parse html content to get icon uri.
            if (string.IsNullOrWhiteSpace(feedInfo.IconUri))
            {
                string uri = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(feedInfo.WebsiteLink))
                    {
                        uri = feedInfo.WebsiteLink;
                    }
                    else
                    {
                        uri = new Uri(feedInfo.OriginalUri).GetLeftPart(UriPartial.Authority);
                    }

                    var response = await httpClient.GetAsync(uri);
                    feedInfo.IconUri = new HtmlParser(await response.Content.ReadAsStringAsync(), response.RequestMessage.RequestUri.ToString()).ShortcurtIcon;
                }
                catch (HttpRequestException ex)
                {
                    log.LogError($"Try download the content of {uri} failed. Ex: {ex.Message}");
                }
                catch (Exception ex)
                {
                    log.LogError($"Parse html content of {uri} failed. Ex: {ex.Message}");
                }
            }

            // Try to get the favicon.ico file directly, like https://www.cagle.com, we can't get any icon info from the index page, but it has favicon.ico file.
            if (string.IsNullOrWhiteSpace(feedInfo.IconUri))
            {
                string uri = null;
                try
                {
                    var uriBuilder = new UriBuilder(string.IsNullOrWhiteSpace(feedInfo.WebsiteLink) ? feedInfo.OriginalUri : feedInfo.WebsiteLink);
                    uriBuilder.Path = "/favicon.ico";
                    uri = uriBuilder.Uri.ToString();
                    var response = await httpClient.GetAsync(uriBuilder.Uri);
                    if (response.IsSuccessStatusCode)
                    {
                        feedInfo.IconUri = uri;
                    }
                }
                catch (HttpRequestException ex)
                {
                    log.LogError($"Try download the content of {uri} failed. Ex: {ex.Message}");
                }
            }

            // Save feed info to table.
            feed.Description = feedInfo.Description;
            feed.IconUri = feedInfo.IconUri;
            feed.LastUpdateTimeInUtc = DateTime.UtcNow;
            feed.Name = feedInfo.Name;
            feed.WebSiteUri = feedInfo.WebsiteLink;

            // Parse feed items.
            var feedItems = parser.ParseFeedItems();

            // Save feed items to db.
            var db = dbFactory.CreateDbContext();
            foreach (var item in feedItems)
            {
                var feedItemId = item.PermentLink.Sha256();
                if (await db.FeedItems.FindAsync(feedItemId) == null)
                {
                    db.FeedItems.Add(new ServerCore.Models.FeedItem
                    {
                        Content = item.Content,
                        FeedId = feed.Id,
                        Id = feedItemId,
                        PublishTimeInUtc = item.PubDate.ToUniversalTime(),
                        Summary = item.Summary,
                        Title = item.Title,
                        TopicPictureUri = item.TopicPictureUri,
                        Uri = item.PermentLink
                    });
                }
            }
            await db.SaveChangesAsync();

            // Save feed items to table.
            /*var batch = new TableBatchOperation();
            foreach (var item in feedItems)
            {
                item.PartitionKey = feedUriHash;
                item.RowKey = $"{string.Format("{0:D19}", DateTime.MaxValue.Ticks - item.PubDate.ToUniversalTime().Ticks)}-{item.PermentLink.Sha256()}";
                batch.Add(TableOperation.InsertOrMerge(item));
                if (batch.Count == 100)
                {
                    await feedItemsTable.ExecuteBatchAsync(batch);
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
            {
                await feedItemsTable.ExecuteBatchAsync(batch);
            }

            // Svae to latest feed items table.
            batch.Clear();
            foreach (var item in feedItems)
            {
                batch.Add(TableOperation.InsertOrMerge(new FeedItemExEntity(item, feed)
                {
                    PartitionKey = feed.Category ?? "Default",
                    RowKey = item.RowKey,
                }));
                if (batch.Count == 100)
                {
                    await latestFeedItemsTable.ExecuteBatchAsync(batch);
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
            {
                await latestFeedItemsTable.ExecuteBatchAsync(batch);
            }*/

            log.LogInformation($"UpdateFeed: {feed.Uri} finished");
        }
    }
}
