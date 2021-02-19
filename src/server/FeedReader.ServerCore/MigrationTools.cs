using FeedReader.Backend.Share.Entities;
using FeedReader.ServerCore.Datas;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.EntityFrameworkCore;

namespace FeedReader.ServerCore
{
    public static class MigrationTools
    {
        public static async void MoveFeedTableFromAzureToDb(IDbContextFactory<FeedReaderDbContext> dbFactory)
        {
            var db = dbFactory.CreateDbContext();
            var feedTable = AzureStorage.MigrationOnly_GetAzureFeedsTable();
            var feeds = feedTable.ExecuteQuery(new TableQuery<FeedInfoEntity>());
            int count = 0;
            foreach (var feed in feeds)
            {
                var feedInDb = await db.Feeds.FindAsync(feed.RowKey);
                if (feedInDb != null)
                {
                    continue;
                }

                db.Feeds.Add(new Models.Feed
                {
                    Id = feed.RowKey,
                    Category = feed.Category,
                    WebSiteUri = feed.WebsiteLink,
                    Description = feed.Description,
                    IconUri = feed.IconUri,
                    Name = feed.Name,
                    Uri = feed.OriginalUri,
                });

                if (++count == 50)
                {
                    await db.SaveChangesAsync();
                    count = 0;
                }
            }
            await db.SaveChangesAsync();
        }
    }
}
