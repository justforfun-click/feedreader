using FeedReader.ServerCore.Datas;
using FeedReader.WebApi.Entities;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.EntityFrameworkCore;
using System;

namespace FeedReader.ServerCore
{
    public static class MigrationTools
    {
        public static async void MoveUserFeedsFromAzureToDb(IDbContextFactory<FeedReaderDbContext> dbFactory)
        {
            var db = dbFactory.CreateDbContext();
            var userFeedsTable = AzureStorage.MigrationOnly_GetUsersFeedsTable();
            var userFeeds = userFeedsTable.ExecuteQuery(new TableQuery<UserFeedEntity>());
            foreach (var userFeed in userFeeds)
            {
                var uid = userFeed.PartitionKey.Substring(16);
                var userFeedInDb = await db.UserFeeds.FindAsync(userFeed.PartitionKey.Substring(16), userFeed.RowKey);
                if (userFeedInDb != null)
                {
                    continue;
                }

                var user = await db.Users.FindAsync(uid);
                if (user == null)
                {
                    System.Console.WriteLine($"Can't find user: {uid}");
                    continue;
                }

                var feed = await db.Feeds.FindAsync(userFeed.RowKey);
                if (feed == null)
                {
                    System.Console.WriteLine($"Can't find feed: {userFeed.Uri}");
                    continue;
                }

                userFeedInDb = new Models.UserFeed
                {
                    UserId = uid,
                    FeedId = userFeed.RowKey,
                    Group = userFeed.Group,
                };
                if (userFeed.LastReadedTime != null)
                {
                    userFeedInDb.LastReadedTimeInUtc = ((DateTime)userFeed.LastReadedTime).ToUniversalTime();
                }
                db.UserFeeds.Add(userFeedInDb);
            }
            await db.SaveChangesAsync();
        }
    }
}
