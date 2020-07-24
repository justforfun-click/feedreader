using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.WebApi.Extensions;
using FeedReader.Share.DataContracts;
using System.Web.Http;
using FeedReader.WebApi.Processors;

namespace FeedReader.WebApi.Functions
{
    public static class FeedRefresh
    {
        [FunctionName("FeedRefresh")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feed/refresh")] HttpRequest req,
            [Authentication] User user,
            ILogger log)
        {
            var feedUri = req.Query["feed-uri"];
            if (string.IsNullOrWhiteSpace(feedUri))
            {
                return new BadRequestErrorMessageResult("'feed-uri' parameter is missing.");
            }

            return new OkObjectResult(await new FeedProcessor().RefreshFeed(feedUri));
        }
    }
}
