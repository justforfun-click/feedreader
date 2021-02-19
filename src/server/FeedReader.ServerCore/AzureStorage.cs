using Azure.Storage.Queues;
using FeedReader.WebApi;
using Microsoft.Azure.Cosmos.Table;
using System;

namespace FeedReader.ServerCore
{
    public static class AzureStorage
    {
        const string REFRESH_FEED_JOBS_QUEUE = "feedrefreshjobs";

        const string FEED_ITEMS_TABLE = "feeditems";

        const string LATEST_FEED_ITEMS_TABLE = "latestfeeditems";

        const string USER_STARED_FEED_ITEMS_TABLE = "userstaredfeeditems";

        const string USERS_FEEDS_TABLE = "usersfeeds";

        private static readonly string _conns = Environment.GetEnvironmentVariable(Consts.ENV_KEY_AZURE_STORAGE);
        private static readonly CloudStorageAccount _tableStorageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable(Consts.ENV_KEY_AZURE_STORAGE));
        private static readonly CloudTableClient _tableClient = _tableStorageAccount.CreateCloudTableClient();

        public static CloudTable MigrationOnly_GetAzureFeedsTable()
        {
            return _tableClient.GetTableReference("feeds");
        }

        public static CloudTable GetFeedItemsTable()
        {
            return _tableClient.GetTableReference(FEED_ITEMS_TABLE);
        }

        public static CloudTable GetLatestFeedItemsTable()
        {
            return _tableClient.GetTableReference(LATEST_FEED_ITEMS_TABLE);
        }

        public static CloudTable GetUserStaredFeedItemsTable()
        {
            return _tableClient.GetTableReference(USER_STARED_FEED_ITEMS_TABLE);
        }

        public static QueueClient GetFeedRefreshJobsQueue()
        {
            return new QueueClient(_conns, REFRESH_FEED_JOBS_QUEUE);
        }

        public static CloudTable GetUsersFeedsTable()
        {
            return _tableClient.GetTableReference(USERS_FEEDS_TABLE);
        }
    }
}
