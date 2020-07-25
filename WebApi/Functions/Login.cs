using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FeedReader.WebApi.Extensions;
using FeedReader.WebApi.Datas;
using Microsoft.WindowsAzure.Storage.Table;
using FeedReader.WebApi.Entities;
using JWT.Builder;
using JWT.Algorithms;
using System.Collections.Generic;

namespace FeedReader.WebApi
{
    public static class Login
    {
        private const string FeedReaderUuidPrefix = "feedreader:";

        [FunctionName("Login")]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "login")] HttpRequest req,
            [Authentication(AllowThirdPartyToken = true)] User user,
            [TableStorage] CloudTableClient tableClient,
            ILogger log)
        {
            return HttpFilter.RunAsync(req, async () =>
            {
                // Get feedreader uuid?
                var uuid = await GetFeedReaderUuid(user, tableClient);

                // Login, reget the user data.
                var userTable = tableClient.GetTableReference("users");
                var res = await userTable.ExecuteAsync(TableOperation.Retrieve<UserEntity>(partitionKey: uuid, rowkey: uuid));
                if (res == null || res.Result == null)
                {
                    return new UnauthorizedResult();
                }

                // Generate our jwt token.
                var now = DateTimeOffset.UtcNow;
                var userEntity = (UserEntity)res.Result;
                var token = new JwtBuilder()
                    .WithAlgorithm(new HMACSHA256Algorithm())
                    .WithSecret(Environment.GetEnvironmentVariable(Consts.ENV_KEY_JWT_SECRET))
                    .AddClaim("iss", Consts.FEEDREADER_ISS)
                    .AddClaim("aud", Consts.FEEDREADER_AUD)
                    .AddClaim("uuid", userEntity.Uuid)
                    .AddClaim("iat", now.ToUnixTimeSeconds())
                    .AddClaim("exp", now.AddDays(7).ToUnixTimeSeconds())
                    .Encode();

                // Return user info
                return new OkObjectResult(new Share.DataContracts.User
                {
                    Token = token,
                    Uuid = userEntity.Uuid,
                    Feeds = JsonConvert.DeserializeObject<List<Share.DataContracts.Feed>>(userEntity.Feeds)
                });
            });
        }

        private static async Task<string> GetFeedReaderUuid(User user, CloudTableClient tableClient)
        {
            // If it is feedshub uuid, return directly.
            if (user.Uuid.StartsWith(FeedReaderUuidPrefix))
            {
                return user.Uuid;
            }

            // Not feedreader uuid, try to find from the related uuid index.
            var uuidIndex = tableClient.GetTableReference("relateduuidindex");
            var res = await uuidIndex.ExecuteAsync(TableOperation.Retrieve<RelatedUuidEntity>(partitionKey: user.Uuid, rowkey: user.Uuid));
            if (res?.Result != null)
            {
                return ((RelatedUuidEntity)res.Result).FeedReaderUuid;
            }

            // Not found, let's register it.
            var feedshubUuid = $"{FeedReaderUuidPrefix}uuid:" + Guid.NewGuid().ToString("N").ToLower();
            await uuidIndex.ExecuteAsync(TableOperation.Insert(new RelatedUuidEntity()
            {
                PartitionKey = user.Uuid,
                RowKey = user.Uuid,
                ThirdPartyUUid = user.Uuid,
                FeedReaderUuid = feedshubUuid
            }));

            // Create user.
            var userTable = tableClient.GetTableReference("users");
            var userEntity = new UserEntity()
            {
                PartitionKey = feedshubUuid,
                RowKey = feedshubUuid,
                Uuid = feedshubUuid,
                Email = user.Email,
                Name = user.Name,
                RegistrationTime = DateTime.Now,
                AvatarUrl = user.AvatarUrl
            };
            if (string.IsNullOrWhiteSpace(userEntity.AvatarUrl))
            {
                userEntity.AvatarUrl = $"https://s.gravatar.com/avatar/{user.Email.Md5()}?s=256";
            }
            await userTable.ExecuteAsync(TableOperation.Insert(userEntity));
            return feedshubUuid;
        }
    }
}
