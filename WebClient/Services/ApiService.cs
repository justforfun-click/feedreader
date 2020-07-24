using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FeedReader.WebClient.Models;
using Newtonsoft.Json;

namespace FeedReader.WebClient.Services
{
    public class ApiService
    {
        private readonly HttpClient _http = new HttpClient() { BaseAddress = new Uri("https://feedreaderapi.azurewebsites.net/v1/") };

        public async Task<Share.DataContracts.User> LoginAsync(string token)
        {
            _http.DefaultRequestHeaders.Remove("authentication");
            _http.DefaultRequestHeaders.Add("authentication", token);
            var user = JsonConvert.DeserializeObject<Share.DataContracts.User>(await _http.GetStringAsync("login"));
            _http.DefaultRequestHeaders.Remove("authentication");
            _http.DefaultRequestHeaders.Add("authentication", user.Token);
            return user;
        }

        public async Task<List<Share.DataContracts.Feed>> SubscribeFeed(Feed feed)
        {
            var content = new StringContent(JsonConvert.SerializeObject(new Share.DataContracts.Feed()
            {
                Name = feed.Name,
                Group = feed.Group,
                Uri = feed.Uri
            }), Encoding.UTF8);
            var result = await _http.PostAsync("feed/subscribe", content);
            return JsonConvert.DeserializeObject<List<Share.DataContracts.Feed>>(await result.Content.ReadAsStringAsync());
        }

        public async Task<Share.DataContracts.Feed> RefreshFeed(string feedUri)
        {
            return JsonConvert.DeserializeObject<Share.DataContracts.Feed>(await _http.GetStringAsync($"feed/refresh?feed-uri={HttpUtility.UrlEncode(feedUri)}"));
        }
    }
}
