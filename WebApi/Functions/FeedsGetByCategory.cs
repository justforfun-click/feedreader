using System;
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

namespace FeedReader.WebApi.Functions
{
    public static class FeedsGetByCategory
    {
        [FunctionName("FeedsGetByCategory")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feeds")] HttpRequest req,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                FeedCategory category;
                if (!Enum.TryParse<FeedCategory>(req.Query["category"], out category))
                {
                    throw new ExternalErrorExcepiton("'category' is missing.");
                }

                return new OkObjectResult(await new FeedProcessor().GetFeedsByCategory(
                    category,
                    feedTable: Backend.Share.AzureStorage.GetFeedsTable(),
                    feedItemTable: Backend.Share.AzureStorage.GetFeedItemsTable()
                ));
            });
        }

        [FunctionName("FeedsGetByCategoryV2")]
        public static Task<IActionResult> RunV2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "v2/feeds")] HttpRequest req,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                FeedCategory category;
                if (!Enum.TryParse<FeedCategory>(req.Query["category"], out category))
                {
                    throw new ExternalErrorExcepiton("'category' is missing.");
                }

                return new OkObjectResult(await new FeedProcessor().GetFeedItemsByCategory(category, req.Query["next-row-key"], Backend.Share.AzureStorage.GetLatestFeedItemsTable()));
            });
        }
    }
}
