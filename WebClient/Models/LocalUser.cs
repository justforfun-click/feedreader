using System.Collections.Generic;
using System.Threading.Tasks;
using FeedReader.WebClient.Services;
using System;

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

        public string Token { get; set; }

        public bool IsAuthorized { get { return !string.IsNullOrWhiteSpace(Token); } }

        public List<Feed> Feeds { get; set; } = new List<Feed>();

        public LocalUser(LogService logger, LocalStorageService localStorage, ApiService api)
        {
            _logger = logger;
            _localStorage = localStorage;
            _api = api;
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
            var user = await _api.LoginAsync(token);
            await _localStorage.SetAsync(LOCAL_USER_LOCAL_STORAGE_KEY, Token = user.Token);
            SyncFeeds(user.Feeds);
        }

        public async Task LogoutAsync()
        {
            Token = null;
            await _localStorage.ClearAsync();
        }

        public async Task SubscribeFeedAsync(Feed feed)
        {
            try
            {
                Feeds.Add(feed);
                SyncFeeds(await _api.SubscribeFeed(feed));
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

        private void SyncFeeds(List<Share.DataContracts.Feed> feeds)
        {
            Feeds.Clear();
            feeds.ForEach(f => Feeds.Add(new Feed() {
                Name = f.Name,
                Uri = f.Uri,
                Group = f.Group,
            }));
        }
    }
}
