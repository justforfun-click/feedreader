using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
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
                return await ValidateTokenAsync(token);
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

        private async Task<bool> ValidateTokenAsync(string token)
        {
            // Decode the token.
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwt = jwtHandler.ReadJwtToken(token);
            if (jwt.Issuer == "https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0")
            {
                await ValidateMsTokenAsync(jwtHandler, jwt);
            }

            // Token is valid, save it.
            Token = token;
            await _localStorage.SetAsync(LOCAL_USER_LOCAL_STORAGE_KEY, this);
            return true;
        }

        private async Task ValidateMsTokenAsync(JwtSecurityTokenHandler jwtHandler, JwtSecurityToken jwt)
        {
            // Get Microsoft public keys.
            using (var client = new HttpClient())
            {
                var keys = JsonConvert.DeserializeObject<MicrosoftKeys>(await client.GetStringAsync("https://login.microsoftonline.com/common/discovery/v2.0/keys"));
                var key = keys.Keys.Where(k => k.Kid == (string)jwt.Header["kid"]).First();
                var signCert = new SigningCredentials(new X509SecurityKey(new X509Certificate2(Base64UrlEncoder.DecodeBytes(key.X5c.First()))), "RSA256");

                // Validate token.
                SecurityToken token;
                var principal = jwtHandler.ValidateToken(jwt.RawData, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidAudience =  CLIENT_ID,
                    IssuerSigningKey = signCert.Key
                }, out token);
            }
        }
    }
}
