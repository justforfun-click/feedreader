using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FeedReader.WebApi.Extensions;
using FeedReader.Share.DataContracts;
using System.Web.Http;
using FeedReader.WebApi.Entities;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Generic;

namespace FeedReader.WebApi.Functions
{
    public static class FeedMarkAsReaded
    {
        [FunctionName("FeedMarkAsReaded")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "feed/mark_as_readed")] HttpRequest req,
            [Authentication] User user,
            [TableStorage] CloudTableClient tableClient,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                var feedItemUri = req.Query["feed-item-uri"];
                if (string.IsNullOrWhiteSpace(feedItemUri))
                {
                    return new BadRequestErrorMessageResult("'feed-item-uri' parameter is missing.");
                }
                else
                {
                    feedItemUri = feedItemUri.ToString().ToLower();
                }

                // Compute hash.
                var hash = Share.Utils.Md5(feedItemUri);

                // Retrive current user's readed hashs.
                SortedSet<string> readedHashs = null;
                var userTable = tableClient.GetTableReference("users");
                var userEntity = (UserEntity)(await userTable.ExecuteAsync(TableOperation.Retrieve<UserEntity>(partitionKey: user.Uuid, rowkey: user.Uuid))).Result;
                if (!string.IsNullOrWhiteSpace(userEntity.ReadedHashs))
                {
                    readedHashs = JsonConvert.DeserializeObject<SortedSet<string>>(userEntity.ReadedHashs);
                }
                else
                {
                    readedHashs = new SortedSet<string>();
                }

                // Add to list
                if (readedHashs.Add(hash))
                {
                    userEntity.ReadedHashs = JsonConvert.SerializeObject(readedHashs);
                    await userTable.ExecuteAsync(TableOperation.Replace(userEntity));
                }

                // Return success.
                return new OkResult();
            });
        }
    }
}
