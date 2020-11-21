using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Extensions;
using System.Web.Http;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Processors;

namespace FeedReader.WebApi.Functions
{
    public static class FeedSubscribe
    {
        [FunctionName("FeedSubscribe")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "feed/subscribe")] HttpRequest req,
            [Authentication] UserEntity user,
            [HttpRequestContent(Type = typeof(Feed))] Feed feed,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                if (feed == null)
                {
                    return new BadRequestErrorMessageResult("feed is missed.");
                }

                if (string.IsNullOrWhiteSpace(feed.OriginalUri))
                {
                    return new BadRequestErrorMessageResult("feed original uri is required.");
                }
                else
                {
                    feed.OriginalUri = feed.OriginalUri.Trim();
                }

                var usersFeedsTable = AzureStorage.GetUsersFeedsTable();
                var feedTable = Backend.Share.AzureStorage.GetFeedsTable();
                return new ObjectResult(await new FeedProcessor().SubscribeFeedAsync(feed.OriginalUri, feed.Name, feed.Group, user.Uuid, usersFeedsTable, feedTable));
            });
        }
    }
}
