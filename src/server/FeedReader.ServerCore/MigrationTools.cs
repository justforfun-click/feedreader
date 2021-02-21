using FeedReader.ServerCore.Datas;
using FeedReader.WebApi.Entities;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.EntityFrameworkCore;
using System;

namespace FeedReader.ServerCore
{
    public static class MigrationTools
    {
        public static async void MoveFeedItemsFromAzureToDb(IDbContextFactory<FeedReaderDbContext> dbFactory)
        {
            // TODO
        }
    }
}
