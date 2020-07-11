using Newtonsoft.Json;
using System.Threading.Tasks;
using FeedReader.WebClient.Services;

namespace FeedReader.WebClient.Models
{
    public class LocalUser
    {
        private const string LOCAL_USER_LOCAL_STORAGE_KEY = "feedreader.local_user";
        
        private readonly LogService _logger;

        private readonly LocalStorageService _localStorage;

        [JsonProperty]
        private string Token { get; set; }

        [JsonIgnore]
        public bool IsAuthorized { get { return !string.IsNullOrWhiteSpace(Token); } }

        public LocalUser(LogService logger, LocalStorageService localStorage)
        {
            _logger = logger;
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var localUser = await _localStorage.GetAsync<LocalUser>(LOCAL_USER_LOCAL_STORAGE_KEY);
                Token = localUser?.Token;
            }
            catch (JsonException ex)
            {
                _logger.Error($"Deserialize local user failed, ex: {ex.Message}.");
                await _localStorage.ClearAsync();
            }
        }

        public async Task<string> LoginAsync()
        {
            await Task.Delay(3000);
            Token = "fake token";
            await _localStorage.SetAsync(LOCAL_USER_LOCAL_STORAGE_KEY, this);
            return null;
        }

        public async Task LogoutAsync()
        {
            Token = null;
            await _localStorage.ClearAsync();
            await Task.Delay(3000);
        }
    }
}
