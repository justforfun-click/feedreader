using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Entities;
using FeedReader.WebApi.Extensions;
using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeedReader.Share;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using Azure.Storage.Queues;
using User = FeedReader.ServerCore.Models.User;
using Microsoft.EntityFrameworkCore;
using FeedReader.ServerCore.Datas;

namespace FeedReader.WebApi.Processors
{
    public class UserProcessor : Processor
    {
        const int MAX_RETURN_COUNT = 50;

        private IDbContextFactory<FeedReaderDbContext> _dbFactory;

        public UserProcessor(IDbContextFactory<FeedReaderDbContext> dbFactory, ILogger logger = null)
            : base(logger)
        {
            _dbFactory = dbFactory;
        }

        public async Task<User> LoginAsync(User user)
        {
            var db = _dbFactory.CreateDbContext();

            // If it is feedreader user already (has id property), query user in db.
            if (!string.IsNullOrEmpty(user.Id))
            {
                user = await db.Users.Include(u => u.Feeds).ThenInclude(f => f.Feed).FirstOrDefaultAsync(u => u.Id == user.Id);
                if (user == null)
                {
                    throw new UnauthorizedAccessException();
                }
            }
            else
            {
                // Not feedreader uuid, try to find from the related uuid index.
                var dbUser = await db.Users.Include(u => u.Feeds).ThenInclude(f => f.Feed).FirstOrDefaultAsync(u => u.ThirdPartyId == user.ThirdPartyId);
                if (dbUser != null)
                {
                    user = dbUser;
                }
                else
                {
                    // Not found, let's register it.
                    user.Id = Guid.NewGuid().ToString("N").ToLower();
                    user.RegistrationTimeInUtc = DateTime.UtcNow;
                    db.Users.Add(user);
                }
            }

            // Update last active time.
            user.LastActiveTimeInUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Generate our jwt token.
            var now = DateTimeOffset.UtcNow;
            user.Token = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Environment.GetEnvironmentVariable(Consts.ENV_KEY_JWT_SECRET))
                .AddClaim("iss", Consts.FEEDREADER_ISS)
                .AddClaim("aud", Consts.FEEDREADER_AUD)
                .AddClaim("uid", user.Id)
                .AddClaim("iat", now.ToUnixTimeSeconds())
                .AddClaim("exp", now.AddDays(7).ToUnixTimeSeconds())
                .Encode();
            return user;
        }

        public async Task StarFeedItemAsync(FeedItem feedItem, User user)
        {
            var db = _dbFactory.CreateDbContext();
            db.UserFavorites.Add(new ServerCore.Models.UserFavorite
            {
                UserId = user.Id,
                FavoriteItemIdHash = feedItem.PermentLink.Md5(),
                FeedItemId = feedItem.PermentLink.Sha256()
            });
            await db.SaveChangesAsync();
        }

        public async Task UnstarFeedItemAsync(string feedItemPermentLink, DateTime pubDate, User user)
        {
            // Remove from the star items table.
            var db = _dbFactory.CreateDbContext();
            var favoritItem = new ServerCore.Models.UserFavorite
            {
                UserId = user.Id,
                FavoriteItemIdHash = feedItemPermentLink.Md5()
            };
            db.UserFavorites.Attach(favoritItem);
            db.UserFavorites.Remove(favoritItem);
            await db.SaveChangesAsync();
        }

        public async Task<List<ServerCore.Models.FeedItem>> GetStaredFeedItemsAsync(string nextRowKey, User user)
        {
            var db = _dbFactory.CreateDbContext();
            return await db.UserFavorites.Where(f => f.UserId == user.Id).Include(f => f.FeedItem).ThenInclude(f => f.Feed).Select(f => f.FeedItem).ToListAsync();
        }

        public async Task MarkItemsAsReaded(User user, string feedUri, DateTime lastReadedTime)
        {
            var feedId = Utils.Sha256(feedUri);
            var db = _dbFactory.CreateDbContext();
            var userFeed = await db.UserFeeds.FindAsync(user.Id, feedId);
            if (userFeed == null)
            {
                throw new ExternalErrorExcepiton("'feedUri' is not found.");
            }

            // Update with latest feed info.
            userFeed.LastReadedTimeInUtc = lastReadedTime.ToUniversalTime();
            await db.SaveChangesAsync();
        }
    }
}
