using JWT.Builder;
using JWT.Algorithms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FeedReader.WebClient.Services;
using System.Web;

namespace FeedReader.WebClient.Models
{
    public class LocalUser
    {
        private const string LOCAL_USER_LOCAL_STORAGE_KEY = "feedreader.local_user";

        private const string CLIENT_ID = "dcaaa2ba-a614-4b8c-b78e-1fb39cb8899a";

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

        public string GetMsLoginUri(string redirectUri)
        {
            return $"https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id={CLIENT_ID}&redirect_uri={redirectUri}&response_type=id_token&scope=openid+profile+email&nonce=feedreader";
        }

        public async Task<bool> LoginWithMsCallbacAsync(string callbackUri)
        {
            try
            {
                // Get jwt token from the uri.
                var fragment = callbackUri.Substring(callbackUri.IndexOf('#') + 1);
                var queries = HttpUtility.ParseQueryString(fragment);
                var token = queries["id_token"];
                if (token == null)
                {
                    var error = queries["error"];
                    if (error == null)
                    {
                        _logger.Error($"Unexpected ms-callback, uri: {callbackUri}");
                    }
                    else
                    {
                        _logger.Error($"Get error from ms login callbac: {error}");
                    }
                    return false;
                }

                // Validate token.
                await ValidateMicrosoftTokenAsync(token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Verify token failed, ex: {ex.Message}");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            Token = null;
            await _localStorage.ClearAsync();
        }

        private async Task ValidateMicrosoftTokenAsync(string token)
        {
            // Decode the token.
            var header = new JwtBuilder().DoNotVerifySignature().DecodeHeader<IDictionary<string, string>>(token);
            var kid = GetRequiredFieldInTheToken(header, "kid");
            
            // Get Microsoft public keys.
            using (var client = new HttpClient())
            {
                var keys = JsonConvert.DeserializeObject<MicrosoftKeys>(await client.GetStringAsync("https://login.microsoftonline.com/common/discovery/v2.0/keys"));
                var key = keys.Keys.Where(k => k.Kid == kid).First();
                var cert = new X509Certificate2(Convert.FromBase64String(key.X5c.First()), "RSA256");

                // Validate token.
                var payload = new JwtBuilder().WithAlgorithm(new RS256Algorithm(cert)).MustVerifySignature().Decode<IDictionary<string, string>>(token);

                // Verify iss is microsoft.
                if (GetRequiredFieldInTheToken(payload, "iss") != "https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0")
                {
                    throw new ArgumentException("The iss in token is not Microsoft.");
                }

                // Verify aud is our client.
                if (GetRequiredFieldInTheToken(payload, "aud") != CLIENT_ID)
                {
                    throw new ArgumentException("The aud in token is not feedreader.org.");
                }

                // Verify email address is present.
                GetRequiredFieldInTheToken(payload, "email");

                // Token is valid, save it.
                Token = token;
                await _localStorage.SetAsync(LOCAL_USER_LOCAL_STORAGE_KEY, this);
            }
        }

        private string GetRequiredFieldInTheToken(IDictionary<string, string> payload, string key)
        {
            if (!payload.ContainsKey(key) || string.IsNullOrEmpty(payload[key]))
            {
                throw new ArgumentException($"The '{key}' can't be found in the token.");
            }
            return payload[key].Trim();
        }
    }
}
