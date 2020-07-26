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
using Microsoft.WindowsAzure.Storage.Table;
using FeedReader.WebApi.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace FeedReader.WebApi.Functions
{
    public static class FeedRefresh
    {
        [FunctionName("FeedRefresh")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feed/refresh")] HttpRequest req,
            [Authentication] User user,
            [TableStorage] CloudTableClient tableClient,
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

                var feed = await new FeedProcessor().RefreshFeed(feedUri);
                if (string.IsNullOrWhiteSpace(feed.Error) && feed.Items != null)
                {
                    // get user readed hashs.
                    var userTable = tableClient.GetTableReference("users");
                    var userEntity = (UserEntity)(await userTable.ExecuteAsync(TableOperation.Retrieve<UserEntity>(partitionKey: user.Uuid, rowkey: user.Uuid))).Result;
                    if (!string.IsNullOrWhiteSpace(userEntity.ReadedHashs))
                    {
                        var readedHashs = JsonConvert.DeserializeObject<SortedSet<string>>(userEntity.ReadedHashs);
                        if (readedHashs.Count > 0)
                        {
                            foreach (var feedItem in feed.Items)
                            {
                                if (readedHashs.Contains(Share.Utils.Md5(feedItem.PermentLink)))
                                {
                                    feedItem.IsReaded = true;
                                }
                            }
                        }
                    }
                }
                return new OkObjectResult(feed);
            });
        }
    }
}
