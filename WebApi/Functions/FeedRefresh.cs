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
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feed/refresh")] HttpRequest req,
            [Authentication] User user,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                var feedUri = req.Query["feed-uri"];
                if (string.IsNullOrWhiteSpace(feedUri))
                {
                    return new BadRequestErrorMessageResult("'feed-uri' parameter is missing.");
                }
                else
                {
                    feedUri = feedUri.ToString().ToLower();
                }
                return new OkObjectResult(await new FeedProcessor().RefreshFeed(feedUri));
            });
        }
    }
}
