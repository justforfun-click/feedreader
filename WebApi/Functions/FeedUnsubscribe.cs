using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.WebApi.Extensions;
using System.Web.Http;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Processors;

namespace FeedReader.WebApi.Functions
{
    public static class FeedUnsubscribe
    {
        [FunctionName("FeedUnsubscribe")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feed/unsubscribe")] HttpRequest req,
            [Authentication] UserEntity user,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                var feedUri = req.Query["feed-uri"];
                if (string.IsNullOrWhiteSpace(feedUri))
                {
                    return new BadRequestErrorMessageResult("'feed-uri' parameter is misssing.");
                }
                else
                {
                    feedUri = feedUri.ToString().ToLower();
                }
                await new FeedProcessor().UnsubscribeFeedAsync(feedUri, user.Uuid, AzureStorage.GetUsersFeedsTable());
                return new OkResult();
            });
        }
    }
}
