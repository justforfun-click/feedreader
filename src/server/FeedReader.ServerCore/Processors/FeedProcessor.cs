using FeedReader.ServerCore;
using FeedReader.ServerCore.Datas;
using FeedReader.Share.DataContracts;
using FeedReader.WebApi.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using User = FeedReader.ServerCore.Models.User;

namespace FeedReader.WebApi.Processors
{
    public class FeedProcessor
    {
        const int MAX_RETURN_COUNT = 50;

        private IDbContextFactory<FeedReaderDbContext> _dbFactory;
        private readonly HttpClient _httpClient;

        public FeedProcessor(IDbContextFactory<FeedReaderDbContext> dbFactory, HttpClient httpClient = null)
        {
            _dbFactory = dbFactory;
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<Feed> RefreshFeedAsync(string uri, bool noItems = false)
        {
            uri = uri.Trim();
            Feed feed = null;
            try
            {
                var parser = FeedParser.FeedParser.Create(await _httpClient.GetStringAsync(uri));
                feed = parser.ParseFeedInfo().ToFeed();
                if (!noItems)
                {
                    feed.Items = parser.ParseFeedItems().Select(i => i.ToFeedItem()).ToList();
                }
            }
            catch (HttpRequestException)
            {
                feed = new Feed()
                {
                    Error = "The feed uri is not reachable."
                };
            }
            catch (XmlException)
            {
                feed = new Feed()
                {
                    Error = "The feed content is not valid."
                };
            }
            feed.Uri = uri.Trim().ToLower();
            feed.OriginalUri = uri;
            return feed;
        }

        public async Task<Feed> GetFeedItemsAsync(string feedUri, int page, User user)
        {
            var originalUri = feedUri;
            feedUri = feedUri.Trim().ToLower();
            var feedId = Utils.Sha256(feedUri);
            var db = _dbFactory.CreateDbContext();

            // Get feed information.
            // If the user blob is not null, we will get the feed from the user blob because user might customize the group and name on this feed.
            ServerCore.Models.UserFeed userFeed = null;
            ServerCore.Models.Feed feedInDb = null;
            if (user != null)
            {
                userFeed = await db.UserFeeds.Include(f => f.Feed).FirstOrDefaultAsync(u => u.UserId == user.Id && u.FeedId == feedId);
                feedInDb = userFeed?.Feed;
            }

            // If we didn't get the feed, two possibility:
            // 1. The feed is not subscribed by user yet.
            // 2. Anonymous user.
            // No matter for which case, we will try to get the feed info from feed info table directly.
            if (feedInDb == null)
            {
                feedInDb = await db.Feeds.FindAsync(feedId);
            }

            if (feedInDb == null)
            {
                throw new ExternalErrorExcepiton($"Feed '{feedUri}' is not found.");
            }

            var feed = new Feed
            {
                Description = feedInDb.Description,
                IconUri = feedInDb.IconUri,
                Name = feedInDb.Name,
                OriginalUri = feedInDb.Uri,
                Uri = feedUri,
                WebsiteLink = feedInDb.WebSiteUri,
            };
            if (userFeed != null)
            {
                feed.Group = userFeed.Group;
            }

            // Get feed items.
            var feedItems = await db.FeedItems
                .Where(f => f.FeedId == feedId)
                .OrderByDescending(f => f.PublishTimeInUtc)
                .Skip(page * 50)
                .Take(50).ToListAsync();
            if (feedItems.Count > 0)
            {
                feed.Items = feedItems.Select(f => new FeedItem
                {
                    Summary = f.Summary,
                    Content = f.Content,
                    PermentLink = f.Uri,
                    PubDate = f.PublishTimeInUtc,
                    Title = f.Title,
                    TopicPictureUri = f.TopicPictureUri,
                }).ToList();
            }
            else if (page == 0)
            {
                // We haven't refreshed this feed on the server (new added?), get it directly form rss.
                feed = await RefreshFeedAsync(string.IsNullOrWhiteSpace(feed.OriginalUri) ? feed.Uri : feed.OriginalUri);
            }

            // Mark readed or not.
            if (userFeed != null && userFeed.LastReadedTimeInUtc.Ticks != 0)
            {
                foreach (var feedItem in feed.Items)
                {
                    if (feedItem.PubDate <= userFeed.LastReadedTimeInUtc)
                    {
                        feedItem.IsReaded = true;
                    }
                }
            }

            // Mark stared or not.
            if (user != null)
            {
                // Mark stared or not
                var favorites = db.UserFeedItems.Where(f => f.UserId == user.Id && f.IsFavorite).Select(f => f.FeedItemId).ToList();
                if (favorites.Count > 0)
                {
                    foreach (var feedItem in feed.Items)
                    {
                        if (favorites.Find(id => id == feedItem.PermentLink.Sha256()) != null)
                        {
                            feedItem.IsStared = true;
                        }
                    }
                }
            }

            // All done.
            return feed;
        }

        public async Task<Feed> SubscribeFeedAsync(string feedOriginalUri, string customGroup, User user)
        {
            Feed feed = null;
            string feedUriHash = null;
            var db = _dbFactory.CreateDbContext();
            for (var i = 0; i < 10; ++i)
            {
                var feedUri = feedOriginalUri.Trim().ToLower();
                feedUriHash = Utils.Sha256(feedUri);

                // Do we have this feed already?
                var feedInDb = await db.Feeds.FindAsync(feedUriHash);
                if (feedInDb == null)
                {
                    // Get information of this feed.
                    feed = await RefreshFeedAsync(feedOriginalUri, noItems: true);
                    if (feed.Error != null)
                    {
                        // This uri may not be the feed uri. It maybe the webpage, let's try to find out the potential feed uri in this webpage.
                        feedOriginalUri = await DiscoverFeedUriAsync(feedOriginalUri);
                        if (string.IsNullOrWhiteSpace(feedOriginalUri))
                        {
                            break;
                        }
                        continue;
                    }

                    // Save to feed table.
                    db.Feeds.Add(new ServerCore.Models.Feed
                    {
                        Description = feed.Description,
                        IconUri = feed.IconUri,
                        Id = feedUriHash,
                        Name = feed.Name,
                        RegistrationTimeInUtc = DateTime.UtcNow,
                        WebSiteUri = feed.WebsiteLink,
                        Uri = feed.OriginalUri,
                    });

                    try
                    {
                        await db.SaveChangesAsync();
                    }
                    catch (DbUpdateException ex) when (ex.IsUniqueConstraintException())
                    {
                        // Save to ignore.
                    }

                    break;
                }
                else
                {
                    feed = new Feed
                    {
                        Description = feedInDb.Description,
                        IconUri = feedInDb.IconUri,
                        Name = feedInDb.Name,
                        OriginalUri = feedInDb.Uri,
                        Uri = feedUri,
                        WebsiteLink = feedInDb.WebSiteUri,
                    };
                }
            }

            Debug.Assert(feed != null);
            Debug.Assert(feedUriHash != null);

            // User user customized group.
            feed.Group = customGroup;

            // Save to usersfeeds table.
            db.UserFeeds.Add(new ServerCore.Models.UserFeed
            {
                UserId = user.Id,
                FeedId = feedUriHash
            });
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintException())
            {
                // Save to ignore.
            }

            // Return
            return feed;
        }

        public async Task UnsubscribeFeedAsync(string feedUri, User user)
        {
            var feedId = Utils.Sha256(feedUri);
            var userFeed = new ServerCore.Models.UserFeed
            {
                UserId = user.Id,
                FeedId = feedId
            };
            var db = _dbFactory.CreateDbContext();
            db.UserFeeds.Attach(userFeed);
            db.UserFeeds.Remove(userFeed);
            await db.SaveChangesAsync();
        }

        public async Task UpdateFeedAsync(string feedUri, string newFeedGroup, User user)
        {
            // Get the original feed.
            var feedId = Utils.Sha256(feedUri);
            var db = _dbFactory.CreateDbContext();
            var userFeed = await db.UserFeeds.FindAsync(user.Id, feedId);
            if (userFeed == null)
            { 
                throw new ExternalErrorExceptionNotFound();
            }

            userFeed.Group = newFeedGroup;
            await db.SaveChangesAsync();
        }

        private async Task<string> DiscoverFeedUriAsync(string uri)
        {
            try
            {
                var content = await _httpClient.GetStringAsync(uri);
                foreach (var regex in HtmlFeedRegexes)
                {
                    var match = regex.Match(content);
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
                return null;
            }
            catch
            {
                // Ignore all erros.
                return null;
            }
        }

        private static Regex[] HtmlFeedRegexes;
        
        static FeedProcessor()
        {
            HtmlFeedRegexes = new Regex[]
            {
                new Regex("<link [^>]*type\\s*=\\s*['\"]application\\/rss\\+xml['\"][^>]*href\\s*=\\s*['\"]([^'\"]*)['\"]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                new Regex("<link [^>]*href\\s*=\\s*['\"]([^'\"]*)['\"][^>]*type\\s*=\\s*['\"]application\\/rss\\+xml['\"]", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            };
        } 
    }
}
