using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using FeedReader.WebApi.Extensions;
using Microsoft.Azure.Cosmos.Table;
using FeedReader.WebApi.Entities;
using FeedReader.ServerCore.Datas;
using FeedReader.ServerCore.Models;
using System.Collections.Generic;
using Newtonsoft.Json;

[assembly: WebJobsStartup(typeof(FeedReader.WebApi.Startup))]

namespace FeedReader.WebApi
{
    class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExtension<AuthenticationExtension>();
            builder.AddExtension<HttpRequestContentExtension>();

#if DEBUG
            AzureStorage.GetUserContainer().CreateIfNotExists();
            AzureStorage.GetRelatedUuidIndexTable().CreateIfNotExists();
            AzureStorage.GetUsersFeedsTable().CreateIfNotExists();
#endif
        }
    }

    public static class MigrationTools
    {
        public static async void MoveUserTableFromAzureToDb()
        {
            var dbFactory = new DesignTimeFeedReaderDbContextFactory();
            var uuidIndexTable = AzureStorage.GetRelatedUuidIndexTable();
            foreach (var relatedUuid in uuidIndexTable.ExecuteQuery<RelatedUuidEntity>(new TableQuery<RelatedUuidEntity>()))
            {
                var db = dbFactory.CreateDbContext(null);
                var uid = relatedUuid.FeedReaderUuid.Substring(16);
                var user = await db.Users.FindAsync(uid);
                if (user != null)
                {
                    continue;
                }

                var userBlob = AzureStorage.GetUserBlob(relatedUuid.FeedReaderUuid);
                var userEntity = await userBlob.GetAsync<UserEntity>();
                db.Add(new User
                {
                    RegistrationTimeInUtc = userEntity.RegistrationTime.ToUniversalTime(),
                    Id = uid,
                    ThirdPartyId = relatedUuid.ThirdPartyUUid
                });

                if (!string.IsNullOrEmpty(userEntity.StaredHashs))
                {
                    var staredHashs = JsonConvert.DeserializeObject<SortedSet<string>>(userEntity.StaredHashs);
                    foreach (var hash in staredHashs)
                    {
                        db.Add(new UserFavorites
                        {
                            UserId = uid,
                            FavoriteItemIdHash = hash
                        });
                    }
                }
                await db.SaveChangesAsync();
            }
        }
    }
}
