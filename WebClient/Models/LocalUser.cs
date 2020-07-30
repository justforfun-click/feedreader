using System.Collections.Generic;
using System.Threading.Tasks;
using FeedReader.WebClient.Services;
using System;
using FeedReader.WebClient.Datas;
using System.Threading;

namespace FeedReader.WebClient.Models
{
    public class LocalUser
    {
        private const string LOCAL_USER_LOCAL_STORAGE_KEY = "feedreader.local_user_token";

        private class MicrosoftKey
        {
            public string Kty { get; set; }

            public string Use { get; set; }

            public string Kid { get; set; }

            public IEnumerable<string> X5c { get; set; }
        }

        private class MicrosoftKeys
        {
            public IEnumerable<MicrosoftKey> Keys { get; set; }
        }

        private readonly LogService _logger;

        private readonly LocalStorageService _localStorage;

        private readonly ApiService _api;

        private readonly FeedService _feedService;

        private CancellationTokenSource _feedRefreshCancellToken;

        public string Token { get; set; }

        public bool IsAuthorized { get { return !string.IsNullOrWhiteSpace(Token); } }

        public List<Feed> Feeds { get; set; } = new List<Feed>();

        public LocalUser(LogService logger, LocalStorageService localStorage, ApiService api, FeedService feedService)
        {
            _logger = logger;
            _localStorage = localStorage;
            _api = api;
            _feedService = feedService;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var token = await _localStorage.GetAsync<string>(LOCAL_USER_LOCAL_STORAGE_KEY);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    await LoginAsync(token);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Deserialize local user failed or login failed, require to re-login ex: {ex.Message}.");
                await _localStorage.ClearAsync();
            }
        }

        public async Task LoginAsync(string token)
        {
            if (_feedRefreshCancellToken != null)
            {
                _feedRefreshCancellToken.Cancel();
            }

            var user = await _api.LoginAsync(token);
            await _localStorage.SetAsync(LOCAL_USER_LOCAL_STORAGE_KEY, Token = user.Token);
            Feeds = user.Feeds;
            var cancelToken = _feedRefreshCancellToken = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                foreach(var feed in user.Feeds)
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }
                    await _feedService.RefreshFeedAsync(feed);
                }
            });
        }

        public async Task LogoutAsync()
        {
            Token = null;
            if (_feedRefreshCancellToken != null)
            {
                _feedRefreshCancellToken.Cancel();
            }
            await _localStorage.ClearAsync();
        }

        public async Task SubscribeFeedAsync(Feed feed)
        {
            try
            {
                Feeds.Add(feed);
                Feeds = await _api.SubscribeFeed(feed);
            }
            catch (Exception ex)
            {
                _logger.Error($"Subscribe feed failed, ex: {ex.Message}");

                // remove from list.
                var existedFeed = Feeds.Find(f => feed.Uri == f.Uri);
                if (existedFeed != null)
                {
                    Feeds.Remove(existedFeed);
                }
            }
        }

        public async Task UnsubscribeFeedAsync(Feed feed)
        {
            try
            {
                Feeds.Remove(Feeds.Find(f => f.Uri == feed.Uri));
                Feeds = await _api.UnsubscribeFeed(feed.Uri);
            }
            catch (Exception ex)
            {
                _logger.Error($"Unsubscribe feed failed, ex: {ex.Message}");

                // Add it back.
                if (Feeds.Find(f => f.Uri == feed.Uri) == null)
                {
                    Feeds.Add(feed);
                }
            }
        }

        public async Task StarFeedItemAsync(FeedItem feedItem)
        {
            try
            {
                feedItem.IsStared = true;
                await _api.StarFeedItemAsync(feedItem);
            }
            catch (Exception ex)
            {
                _logger.Error($"Star feed item failed, ex: {ex.Message}");
                feedItem.IsStared = false;
            }
        }

        public async Task UnstarFeedItemAsync(FeedItem feedItem)
        {
            try
            {
                feedItem.IsStared = false;
                await _api.UnstarFeedItemAsync(feedItem.PermentLink);
            }
            catch (Exception ex)
            {
                _logger.Error($"Unstar feed item failed, ex: {ex.Message}");
                feedItem.IsStared = true;
            }
        }

        public Task<List<FeedItem>> GetStaredFeedItems()
        {
            return _api.GetStaredFeedItems();
        }
    }
}
