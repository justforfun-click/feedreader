using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Extensions;
using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeedReader.Share;
using Microsoft.Extensions.Logging;
using User = FeedReader.ServerCore.Models.User;
using UserFeedItem = FeedReader.ServerCore.Models.UserFeedItem;
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
            var feedItemId = feedItem.PermentLink.Sha256();
            var userFeedItem = await db.UserFeedItems.FindAsync(user.Id, feedItemId);
            if (userFeedItem == null)
            {
                db.UserFeedItems.Add(new UserFeedItem
                {
                    UserId = user.Id,
                    FeedItemId = feedItemId,
                    IsFavorite = true,
                });
            }
            else
            {
                userFeedItem.IsFavorite = true;
            }
            await db.SaveChangesAsync();
        }

        public async Task UnstarFeedItemAsync(string feedItemPermentLink, DateTime pubDate, User user)
        {
            // Remove from the star items table.
            var db = _dbFactory.CreateDbContext();
            var feedItemId = feedItemPermentLink.Sha256();
            var userFeedItem = await db.UserFeedItems.FindAsync(user.Id, feedItemId);
            if (userFeedItem != null)
            {
                userFeedItem.IsFavorite = false;
                await db.SaveChangesAsync();
            }
        }

        public async Task<List<ServerCore.Models.FeedItem>> GetStaredFeedItemsAsync(string nextRowKey, User user)
        {
            var db = _dbFactory.CreateDbContext();
            return await db.UserFeedItems.Where(f => f.UserId == user.Id && f.IsFavorite).Include(f => f.FeedItem).ThenInclude(f => f.Feed).Select(f => f.FeedItem).ToListAsync();
        }

        public async Task MarkItemsAsReaded(User user, string feedUri, DateTime lastReadedTime)
        {
            var feedId = Utils.Sha256(feedUri.Trim().ToLower());
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
