using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.WebApi.Extensions;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Processors;

namespace FeedReader.WebApi.Functions
{
    public static class UserGetStaredFeedItems
    {
        [FunctionName("UserGetStaredFeedItems")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "stars")] HttpRequest req,
            [Authentication] UserEntity user,
            ILogger log)
        {
            return await HttpFilter.RunAsync(req, async () =>
            {
                return new OkObjectResult(await new UserProcessor().GetStaredFeedItemsAsync(req.Query["next-row-key"], user.Uuid, Backend.Share.AzureStorage.GetUserStaredFeedItemsTable()));
            });
        }
    }
}
