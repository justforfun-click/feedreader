using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeedReader.WebClient.Services;
using FeedReader.Share.Extensions;
using System;

namespace FeedReader.WebClient.Models
{
    public class LocalUser : Share.DataContracts.User
    {
        private const string LOCAL_USER_LOCAL_STORAGE_KEY = "feedreader.local_user";

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

        [JsonIgnore]
        public bool IsAuthorized { get { return !string.IsNullOrWhiteSpace(Token); } }

        public List<Feed> Feeds { get; set; } = new List<Feed>();

        public LocalUser(LogService logger, LocalStorageService localStorage, ApiService api)
        {
            _logger = logger;
            _localStorage = localStorage;
            _api = api;

            // Generate test feeds
            Feeds = new List<Feed>()
            {
                new Feed()
                {
                    Name = "C++ Blog",
                    Uri = "https://isocpp.org/blog/rss",
                    Group = "C++",
                },
                new Feed()
                {
                    Name = "Morden C++",
                    Uri = "http://www.modernescpp.com/?format=feed",
                    Group = "C++",
                },
                new Feed()
                {
                    Name = "Fox News - Log Name Test, I'm very long, really very long.",
                    Uri = "http://feeds.foxnews.com/foxnews/national",
                    Group = "News - Long Group Name Test, I'm very long, really very long."
                }
            };
        }

        public async Task InitializeAsync()
        {
            try
            {
                var localUser = await _localStorage.GetAsync<LocalUser>(LOCAL_USER_LOCAL_STORAGE_KEY);
                if (localUser != null && !string.IsNullOrWhiteSpace(localUser.Token))
                {
                    await LoginAsync(localUser.Token);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Deserialize local user failed or login failed, require to re-login ex: {ex.Message}.");
                (new Share.DataContracts.User()).CopyPropertiesTo(this);
                await _localStorage.ClearAsync();
            }
        }

        public async Task LoginAsync(string token)
        {
            var user = await _api.LoginAsync(token);
            user.CopyPropertiesTo(this);
            await _localStorage.SetAsync(LOCAL_USER_LOCAL_STORAGE_KEY, this);
        }

        public async Task LogoutAsync()
        {
            Token = null;
            await _localStorage.ClearAsync();
        }
    }
}
