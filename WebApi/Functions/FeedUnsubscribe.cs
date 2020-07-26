using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FeedReader.WebApi.Extensions;
using FeedReader.Share.DataContracts;
using Microsoft.WindowsAzure.Storage.Table;
using System.Web.Http;
using System.Collections.Generic;
using FeedReader.WebApi.Entities;

namespace FeedReader.WebApi.Functions
{
    public static class FeedUnsubscribe
    {
        [FunctionName("FeedUnsubscribe")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feed/unsubscribe")] HttpRequest req,
            [Authentication] User user,
            [TableStorage] CloudTableClient tableClient,
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

                // Retrive current user's feeds.
                List<Feed> userFeeds = null;
                var userTable = tableClient.GetTableReference("users");
                var userEntity = (UserEntity)(await userTable.ExecuteAsync(TableOperation.Retrieve<UserEntity>(partitionKey: user.Uuid, rowkey: user.Uuid))).Result;
                if (!string.IsNullOrWhiteSpace(userEntity.Feeds))
                {
                    userFeeds = JsonConvert.DeserializeObject<List<Feed>>(userEntity.Feeds);
                    var feed = userFeeds.Find(f => f.Uri == feedUri);
                    if (feed != null)
                    {
                        userFeeds.Remove(feed);
                        userEntity.Feeds = JsonConvert.SerializeObject(userFeeds);
                        await userTable.ExecuteAsync(TableOperation.Replace(userEntity));
                    }
                }
                return new OkObjectResult(userEntity.Feeds);
            });
        }
    }
}
