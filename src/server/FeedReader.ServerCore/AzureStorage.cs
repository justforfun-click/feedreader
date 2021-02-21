using Azure.Storage.Queues;
using FeedReader.WebApi;
using Microsoft.Azure.Cosmos.Table;
using System;

namespace FeedReader.ServerCore
{
    public static class AzureStorage
    {
        const string FEED_ITEMS_TABLE = "feeditems";

        const string LATEST_FEED_ITEMS_TABLE = "latestfeeditems";

        const string USER_STARED_FEED_ITEMS_TABLE = "userstaredfeeditems";

        private static readonly string _conns = Environment.GetEnvironmentVariable(Consts.ENV_KEY_AZURE_STORAGE);
        private static readonly CloudStorageAccount _tableStorageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable(Consts.ENV_KEY_AZURE_STORAGE));
        private static readonly CloudTableClient _tableClient = _tableStorageAccount.CreateCloudTableClient();

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
    }
}
