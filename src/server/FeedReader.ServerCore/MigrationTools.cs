using FeedReader.Backend.Share.Entities;
using FeedReader.ServerCore.Datas;
using FeedReader.WebApi.Extensions;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.EntityFrameworkCore;
using System;

namespace FeedReader.ServerCore
{
    public static class MigrationTools
    {
        public static async void MoveFavoriteItemsFromAzureToDb(IDbContextFactory<FeedReaderDbContext> dbFactory)
        {
            var db = dbFactory.CreateDbContext();
            var userStarsTable = AzureStorage.Mitigration_GetUserStaredFeedItemsTable();
            var starItems = userStarsTable.ExecuteQuery(new TableQuery<FeedItemExEntity>());
            foreach (var starItem in starItems)
            {
                var userId = starItem.PartitionKey.Substring(16);
                if (await db.Users.FindAsync(userId) == null)
                {
                    Console.WriteLine($"Can't find userid: {userId}");
                    continue;
                }

                var feedItemId = starItem.PermentLink.Sha256();
                if (await db.FeedItems.FindAsync(feedItemId) == null)
                {
                    Console.WriteLine($"Can't find the feeditem: {starItem.PermentLink}");
                    continue;
                }

                var feedItemIdHash = starItem.PermentLink.Md5();
                var userFavorite = await db.UserFavorites.FindAsync(userId, feedItemIdHash);
                if (userFavorite == null)
                {
                    userFavorite = new Models.UserFavorite
                    {
                        UserId = userId,
                        FavoriteItemIdHash = feedItemIdHash,
                    };
                    db.UserFavorites.Add(userFavorite);
                }
                userFavorite.FeedItemId = feedItemId;
            }
            await db.SaveChangesAsync();
        }
    }
}
