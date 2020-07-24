using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
            return JsonConvert.DeserializeObject<Share.DataContracts.User>(await _http.GetStringAsync("login"));
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
    }
}
