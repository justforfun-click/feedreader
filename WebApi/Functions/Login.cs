using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.WebApi.Extensions;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Processors;

namespace FeedReader.WebApi
{
    public static class Login
    {
        [FunctionName("Login")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "login")] HttpRequest req,
            [Authentication(AllowThirdPartyToken = true)] UserEntity user,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                var processor = new UserProcessor();
                var userContainer = AzureStorage.GetUserContainer();
                var uuidIndexTable = AzureStorage.GetRelatedUuidIndexTable();
                var usersFeedsTable = AzureStorage.GetUsersFeedsTable();
                return new OkObjectResult(await processor.LoginAsync(user, userContainer, uuidIndexTable, usersFeedsTable));
            });
        }
    }
}
