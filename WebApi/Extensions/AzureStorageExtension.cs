using System;
using System.Net;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Azure.Storage.Blobs;
using System.IO;
using System.Text;

namespace FeedReader.WebApi.Extensions
{
    static class AzureStorage
    {
        const string RELATED_UUID_INDEX_TABLE = "relateduuidindex";

        const string USERS_CONTAINER = "users";

        const string USERS_FEEDS_TABLE = "usersfeeds";

        const string FEEDS_TABLE = "feeds";

        private static readonly string _conns = Environment.GetEnvironmentVariable(Consts.ENV_KEY_AZURE_STORAGE);

        public static CloudStorageAccount GetStorageAccount()
        {
            return CloudStorageAccount
                        .Parse(Environment.GetEnvironmentVariable(Consts.ENV_KEY_AZURE_STORAGE));
        }

        public static CloudTable GetRelatedUuidIndexTable()
        {
            return GetStorageAccount().CreateCloudTableClient().GetTableReference(RELATED_UUID_INDEX_TABLE);
        }

        public static CloudTable GetUsersFeedsTable()
        {
            return GetStorageAccount().CreateCloudTableClient().GetTableReference(USERS_FEEDS_TABLE);
        }

        public static CloudTable GetFeedsTable()
        {
            return GetStorageAccount().CreateCloudTableClient().GetTableReference(FEEDS_TABLE);
        }

        public static BlobContainerClient GetUserContainer()
        {
            return new BlobContainerClient(_conns, USERS_CONTAINER);
        }

        public static BlobClient GetUserBlob(string uuid)
        {
            return GetUserContainer().GetBlobClient(uuid);
        }
    }

    public static class CloudBlockBlobExtension
    {
        public static async Task<T> GetAsync<T>(this BlobClient blob)
        {
            try
            {
                using(var memStream = new MemoryStream())
                {
                    await blob.DownloadToAsync(memStream);
                    var content = Encoding.UTF8.GetString(memStream.ToArray());
                    return JsonConvert.DeserializeObject<T>(content);
                }
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
            {
                return default(T);
            }
        }

        public static async Task SaveAsync<T>(this BlobClient blob, T obj)
        {
            var content = JsonConvert.SerializeObject(obj);
            using (var memStream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                await blob.UploadAsync(memStream, overwrite: true);
            }
        }
    }
}
