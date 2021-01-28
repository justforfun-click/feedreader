using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using System.Net.Http;
using FeedReader.Backend.Share;
using FeedReader.Backend.Share.FeedParsers;
using FeedReader.WebApi.Processors;
using FeedReader.Backend.Share.Entities;
using FeedReader.Share;

namespace FeedReader.WebApi.AdminFunctions
{
    public static class UpdateFeedFunc
    {
        public static async Task UpdateFeed(string feedUri, CloudTable feedsTable, CloudTable feedItemsTable, CloudTable latestFeedItemsTable, ILogger log, HttpClient httpClient = null)
        {
            // Get original uri.
            feedUri = feedUri.Trim().ToLower();
            var feedUriHash = Utils.Sha256(feedUri);
            var res = await feedsTable.ExecuteAsync(TableOperation.Retrieve<FeedInfoEntity>(partitionKey: "feed_info", rowkey: feedUriHash));
            if (res?.Result == null)
            {
                throw new ExternalErrorExceptionNotFound();
            }
            var feedOriginalUri = ((FeedInfoEntity)res.Result).OriginalUri;

            // Get feed content.
            if (httpClient == null)
            {
                var handler = new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.All, AllowAutoRedirect = true };
                httpClient = new HttpClient(handler);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "FeedReader");
            }

            var fetchUri = feedOriginalUri + (feedOriginalUri.IndexOf('?') > 0 ? "&" : "?") + $"_feedreader_fetch_timestamp={DateTime.Now.ToUniversalTime().Ticks}";
            var feedContent = await httpClient.GetStringAsync(fetchUri);

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
            feedInfo.PartitionKey = "feed_info";
            feedInfo.RowKey = feedUriHash;
            feedInfo.Uri = feedUri;
            feedInfo.OriginalUri = feedOriginalUri;
            await feedsTable.ExecuteAsync(TableOperation.InsertOrMerge(feedInfo));

            // Retrive info, we need the category property.
            feedInfo = (FeedInfoEntity)(await feedsTable.ExecuteAsync(TableOperation.Retrieve<FeedInfoEntity>(partitionKey: "feed_info", rowkey: feedUriHash))).Result;

            // Parse feed items.
            var feedItems = parser.ParseFeedItems();

            // Save feed items to table.
            var batch = new TableBatchOperation();
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
                batch.Add(TableOperation.InsertOrMerge(new FeedItemExEntity(item, feedInfo)
                {
                    PartitionKey = feedInfo.Category ?? "Default",
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
            }
        }
    }
}
