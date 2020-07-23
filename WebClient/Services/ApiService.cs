using System;
using System.Net.Http;
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
    }
}
