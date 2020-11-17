using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Extensions;
using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Processors;
using System;

namespace FeedReader.WebApi.Functions
{
    public static class UserStarUnstarFeedItem
    {
        [FunctionName("UserStarFeedItem")]
        public static async Task<IActionResult> StarFeedItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "star")] HttpRequest req,
            [Authentication] UserEntity user,
            [HttpRequestContent(Type = typeof(FeedItem))] FeedItem feedItem,
            ILogger log)
        {
            return await HttpFilter.RunAsync(req, async () =>
            {
                if (string.IsNullOrWhiteSpace(feedItem.PermentLink))
                {
                    throw new ExternalErrorExcepiton("'PermentLink' of feed item is missing.");
                }
                else
                {
                    feedItem.PermentLink = feedItem.PermentLink.Trim();
                }

                if (string.IsNullOrWhiteSpace(feedItem.FeedUri))
                {
                    throw new ExternalErrorExcepiton("'FeedUri' of feed item is missing.");
                }
                else
                {
                    feedItem.FeedUri = feedItem.FeedUri.Trim().ToLower();
                }

                await new UserProcessor().StarFeedItemAsync(feedItem, AzureStorage.GetUserBlob(user.Uuid), Backend.Share.AzureStorage.GetUserStaredFeedItemsTable());
                return new OkResult();
            });
        }

        [FunctionName("UserUnstarFeedItem")]
        public static async Task<IActionResult> UnstarFeedItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "unstar")] HttpRequest req,
            [Authentication] UserEntity user,
            ILogger log)
        {
            return await HttpFilter.RunAsync(req, async () =>
            {
                var feedItemUri = (string)req.Query["feed-item-uri"];
                if (string.IsNullOrWhiteSpace(feedItemUri))
                {
                    throw new ExternalErrorExcepiton("'feed-item-uri' is missing.");
                }
                else
                {
                    feedItemUri = feedItemUri.Trim();
                }

                DateTime pubDate;
                if (!DateTime.TryParse(req.Query["feed-item-pub-date"], out pubDate))
                {
                    throw new ExternalErrorExcepiton("'feed-item-pub-date' is not valid.");
                }
                else if (pubDate.Kind == DateTimeKind.Unspecified)
                {
                    throw new ExternalErrorExcepiton("'feed-item-pub-date' is not UTC time.");
                }

                await new UserProcessor().UnstarFeedItemAsync(feedItemUri, pubDate, AzureStorage.GetUserBlob(user.Uuid), Backend.Share.AzureStorage.GetUserStaredFeedItemsTable());
                return new OkResult();
            });
        }
    }
}
