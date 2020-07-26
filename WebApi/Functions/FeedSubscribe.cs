using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Extensions;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage.Table;
using FeedReader.WebApi.Entities;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using FeedReader.WebApi.Processors;

namespace FeedReader.WebApi.Functions
{
    public static class FeedSubscribe
    {
        [FunctionName("FeedSubscribe")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "feed/subscribe")] HttpRequest req,
            [Authentication] User user,
            [HttpRequestContent(Type = typeof(Feed))] Feed feed,
            [TableStorage] CloudTableClient tableClient,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                if (feed == null)
                {
                    return new BadRequestErrorMessageResult("feed is missed.");
                }

                if (string.IsNullOrEmpty(feed.Uri))
                {
                    return new BadRequestErrorMessageResult("feed uri is required.");
                }
                else
                {
                    feed.Uri = feed.Uri.Trim().ToLower();
                }

                // Retrive current user's feeds.
                List<Feed> userFeeds = null;
                var userTable = tableClient.GetTableReference("users");
                var userEntity = (UserEntity)(await userTable.ExecuteAsync(TableOperation.Retrieve<UserEntity>(partitionKey: user.Uuid, rowkey: user.Uuid))).Result;
                if (!string.IsNullOrEmpty(userEntity.Feeds))
                {
                    userFeeds = JsonConvert.DeserializeObject<List<Feed>>(userEntity.Feeds);
                }
                else
                {
                    userFeeds = new List<Feed>();
                }

                // Delete the old item.
                var oldItem = userFeeds.FirstOrDefault(f => f.Uri == feed.Uri);
                if (oldItem != null)
                {
                    userFeeds.Remove(oldItem);
                }

                // Get information of this feed.
                var res = await new FeedProcessor().RefreshFeedAsync(feed.Uri, noItems: true);

                // Use user customized name.
                if (!string.IsNullOrWhiteSpace(feed.Name))
                {
                    res.Name = feed.Name;
                }

                // User user customized group.
                res.Group = feed.Group;

                // Add the new item.
                userFeeds.Add(res);

                // Save to table.
                userEntity.Feeds = JsonConvert.SerializeObject(userFeeds);
                await userTable.ExecuteAsync(TableOperation.Replace(userEntity));

                // return new subscribed feeds to client.
                return new OkObjectResult(userEntity.Feeds);
            });
        }
    }
}
