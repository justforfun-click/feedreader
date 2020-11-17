using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.WebApi.Extensions;
using FeedReader.WebApi.Entities;
using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Processors;

namespace FeedReader.WebApi.Functions
{
    public static class FeedUpdate
    {
        [FunctionName("FeedUpdate")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "feed/update")] HttpRequest req,
            [Authentication] UserEntity user,
            [HttpRequestContent(Type = typeof(Feed))] Feed feed,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                if (feed == null)
                {
                    throw new ExternalErrorExcepiton("feed is missed.");
                }

                if (string.IsNullOrWhiteSpace(feed.Uri))
                {
                    throw new ExternalErrorExcepiton("feed uri is missed.");
                }
                else
                {
                    feed.Uri = feed.Uri.Trim().ToLower();
                }

                await new FeedProcessor().UpdateFeedAsync(feed.Uri, feed.Name, feed.Group, user.Uuid, AzureStorage.GetUsersFeedsTable());
                return new OkResult();
            });
        }
    }
}
