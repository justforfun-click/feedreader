using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.WebApi.Extensions;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Table;
using System.Linq;

namespace FeedReader.WebApi.AdminFunctions
{
    public static class GetFeedUriListFunc
    {
        [FunctionName("GetFeedUriList")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "@admin/get-feed-uri-list")] HttpRequest req,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                AdminFunctions.VerifyAdminKey(req);
                return new OkObjectResult(await GetFeedUriList(AzureStorage.GetFeedsTable()));
            });
        }

        public static async Task<List<string>> GetFeedUriList(CloudTable feedsTable)
        {
            TableContinuationToken token = null;
            var results = new List<string>();
            do
            {
                var queryResult = await feedsTable.ExecuteQuerySegmentedAsync(new TableQuery()
                {
                    SelectColumns = new List<string>() { "Uri" }
                }, token);

                var items = queryResult?.Results?.Select(r => r["Uri"].StringValue).ToArray();
                if (items != null)
                {
                    results.AddRange(items);
                }
                token = queryResult?.ContinuationToken;
            } while (token != null);
            return results;
        }
    }
}
