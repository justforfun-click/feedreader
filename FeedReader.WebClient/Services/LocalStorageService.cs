using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FeedReader.WebClient.Services
{
    public class LocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async ValueTask<T> GetAsync<T>(string key)
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            return JsonConvert.DeserializeObject<T>(json ?? string.Empty);
        }

        public ValueTask SetAsync<T>(string key, T value)
        {
            return _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, JsonConvert.SerializeObject(value));
        }

        public ValueTask ClearAsync()
        {
            return _jsRuntime.InvokeVoidAsync("localStorage.clear");
        }
    }
}
