using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FeedReader.WebClient.Datas;
using Newtonsoft.Json;
using FeedCategory = FeedReader.Share.DataContracts.FeedCategory;

namespace FeedReader.WebClient.Services
{
    public class ApiService
    {
        #if DEBUG
        private readonly HttpClient _http = new HttpClient() { BaseAddress = new Uri("http://localhost:7071/v1/") };
        #else
        private readonly HttpClient _http = new HttpClient() { BaseAddress = new Uri("https://feedreaderapi.azurewebsites.net/v1/") };
        #endif

        public async Task<User> LoginAsync(string token)
        {
            _http.DefaultRequestHeaders.Remove("authentication");
            _http.DefaultRequestHeaders.Add("authentication", token);
            var user = await GetAsync<User>("login");
            _http.DefaultRequestHeaders.Remove("authentication");
            _http.DefaultRequestHeaders.Add("authentication", user.Token);
            return user;
        }

        public async Task<List<Feed>> SubscribeFeed(Feed feed)
        {
            return await PostAsync<List<Feed>>("feed/subscribe", new Share.DataContracts.Feed()
            {
                Name = feed.Name,
                Group = feed.Group,
                Uri = feed.Uri,
                OriginalUri = feed.OriginalUri
            });
        }

        public async Task<List<Feed>> UnsubscribeFeed(string feedUri)
        {
            return await GetAsync<List<Feed>>("feed/unsubscribe", new Dictionary<string, string>{
                { "feed-uri", feedUri }
            });
        }

        public async Task<Feed> RefreshFeed(string feedUri, string nextRowKey)
        {
            var args = new Dictionary<string, string> { { "feed-uri", feedUri } };
            if (!string.IsNullOrWhiteSpace(nextRowKey))
            {
                args["next-row-key"] = nextRowKey;
            }
            return await GetAsync<Feed>("feed/refresh", args);
        }

        public async Task MarkAsReaded(List<string> feedItemUris)
        {
            await PosyAsync("feed/mark_as_readed", feedItemUris);
        }

        public async Task StarFeedItemAsync(FeedItem feedItem)
        {
            await PosyAsync("star", feedItem);
        }

        public async Task UnstarFeedItemAsync(string feedItemUri)
        {
            await GetAsync("unstar", new Dictionary<string, string>
            {
                { "feed-item-uri", feedItemUri }
            });
        }

        public async Task<List<FeedItem>> GetStaredFeedItems()
        {
            return await GetAsync<List<FeedItem>>("stars");
        }

        public async Task<List<Feed>> GetFeedsByCategory(FeedCategory feedCategory)
        {
            return await GetAsync<List<Feed>>("feeds", new Dictionary<string, string>
            {
                { "category", feedCategory.ToString() }
            });
        }

        private async Task<TResult> PostAsync<TResult>(string uri, object obj)
        {
            var res = await _http.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8));
            return JsonConvert.DeserializeObject<TResult>(await res.Content.ReadAsStringAsync());
        }

        private async Task PosyAsync(string uri, object obj)
        {
             await _http.PostAsync(uri, new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8));
        }

        private async Task<TResult> GetAsync<TResult>(string uri, Dictionary<string, string> args = null)
        {
            return JsonConvert.DeserializeObject<TResult>(await GetAsync(uri, args));
        }

        private async Task<string> GetAsync(string uri, Dictionary<string, string> args = null)
        {
            if (args == null)
            {
                return await _http.GetStringAsync(uri);
            }
            else
            {
                string.Join("&", args.Select(arg => $"{arg.Key}={HttpUtility.UrlEncode(arg.Value)}"));
                return await _http.GetStringAsync($"{uri}?{string.Join("&", args.Select(arg => $"{arg.Key}={HttpUtility.UrlEncode(arg.Value)}"))}");
            }
        }
    }
}
