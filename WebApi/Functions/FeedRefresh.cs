using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.WebApi.Extensions;
using System.Web.Http;
using FeedReader.WebApi.Processors;
using FeedReader.WebApi.Entities;

namespace FeedReader.WebApi.Functions
{
    public static class FeedRefresh
    {
        [FunctionName("FeedRefresh")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feed/refresh")] HttpRequest req,
            [Authentication(AllowAnonymous = true)] UserEntity user,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                var feedUri = req.Query["feed-uri"];
                if (string.IsNullOrWhiteSpace(feedUri))
                {
                    return new BadRequestErrorMessageResult("'feed-uri' parameter is missing.");
                }

                var userBlob = user == null ? null : AzureStorage.GetUserBlob(user.Uuid);
                return new OkObjectResult(await new FeedProcessor().GetFeedItemsAsync(feedUri, req.Query["next-row-key"], userBlob, AzureStorage.GetUsersFeedsTable(), Backend.Share.AzureStorage.GetFeedsTable(), Backend.Share.AzureStorage.GetFeedItemsTable()));
            });
        }
    }
}
