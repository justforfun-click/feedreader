using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FeedReader.Share.DataContracts;
using Newtonsoft.Json;

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
            var user = JsonConvert.DeserializeObject<Share.DataContracts.User>(await _http.GetStringAsync("login"));
            _http.DefaultRequestHeaders.Remove("authentication");
            _http.DefaultRequestHeaders.Add("authentication", user.Token);
            return user;
        }

        public async Task<List<Feed>> SubscribeFeed(Feed feed)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new Feed()
            {
                Name = feed.Name,
                Group = feed.Group,
                Uri = feed.Uri
            }), Encoding.UTF8);
            var result = await _http.PostAsync("feed/subscribe", content);
            return JsonConvert.DeserializeObject<List<Feed>>(await result.Content.ReadAsStringAsync());
        }

        public async Task<List<Feed>> UnsubscribeFeed(string feedUri)
        {
            return JsonConvert.DeserializeObject<List<Feed>>(await _http.GetStringAsync($"feed/unsubscribe?feed-uri={HttpUtility.UrlEncode(feedUri)}"));
        }

        public async Task<Feed> RefreshFeed(string feedUri)
        {
            return JsonConvert.DeserializeObject<Feed>(await _http.GetStringAsync($"feed/refresh?feed-uri={HttpUtility.UrlEncode(feedUri)}"));
        }

        public async Task MarkAsReaded(string feedItemUri)
        {
            await _http.GetAsync($"feed/mark_as_readed?feed-item-uri={HttpUtility.UrlEncode(feedItemUri)}");
        }
    }
}
