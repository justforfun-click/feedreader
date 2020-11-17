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
using System.Collections.Generic;
using FeedReader.WebApi.Processors;
using System;

namespace FeedReader.WebApi.Functions
{
    public static class FeedMarkAsReaded
    {
        [FunctionName("FeedMarkAsReaded")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "feed/mark_as_readed")] HttpRequest req,
            [Authentication] UserEntity user,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                string feedUri = req.Query["feed-uri"];
                if (string.IsNullOrWhiteSpace(feedUri))
                {
                    throw new ExternalErrorExcepiton("'feed-uri' is missing.");
                }
                else
                {
                    feedUri = feedUri.Trim().ToLower();
                }

                DateTime lastReadedTime;
                if (!DateTime.TryParse(req.Query["last-readed-time"], out lastReadedTime))
                {
                    throw new ExternalErrorExcepiton("'last-readed-time' is not a valid datetime.");
                }
                else if (lastReadedTime.Kind == DateTimeKind.Unspecified)
                {
                    throw new ExternalErrorExcepiton("'last-readed-time' is not UTC time.");
                }

                await new UserProcessor(log).MarkItemsAsReaded(user.Uuid, feedUri, lastReadedTime, AzureStorage.GetUsersFeedsTable(),  Backend.Share.AzureStorage.GetFeedsTable(), Backend.Share.AzureStorage.GetFeedRefreshJobsQueue());
                return new OkResult();
            });
        }
    }
}
