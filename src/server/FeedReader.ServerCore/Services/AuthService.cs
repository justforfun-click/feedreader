using FeedReader.ServerCore.Models;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FeedReader.ServerCore.Services
{
    public interface IAuthService
    {
        Task<User> AuthenticateTokenAsync(string token);
    }

    class AuthService : IAuthService
    {
        public async Task<User> AuthenticateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new UnauthorizedAccessException();
            }
            else if (token.StartsWith(WebApi.Consts.GITHUB_PREFIX))
            {
                return await AuthenticateGitHubAccessCodeAsync(token.Substring(WebApi.Consts.GITHUB_PREFIX.Length));
            }
            else if (token.StartsWith(WebApi.Consts.FACEBOOK_PREFIX))
            {
                return await AuthenticateFacebookAccessTokenAsync(token.Substring(WebApi.Consts.FACEBOOK_PREFIX.Length));
            }

            // Decode it.
            var payload = new JwtBuilder().DoNotVerifySignature().Decode<IDictionary<string, string>>(token);
            var iss = GetValue(payload, "iss");

            // Authenticate token based on issuer.
            switch (iss)
            {
                case WebApi.Consts.FEEDREADER_ISS:
                    return AuthenticateFeedReaderToken(token);

                case WebApi.Consts.MICROSOFT_ISS:
                    return await AuthenticateMsTokenAsync(token);

                case WebApi.Consts.GOOGLE_ISS:
                    return await AuthenticateGoogleTokenAsync(token);

                default:
                    throw new UnauthorizedAccessException();
            }
        }

        private static User AuthenticateFeedReaderToken(string token)
        {
            // Validate the signature.
            var payload = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Environment.GetEnvironmentVariable(WebApi.Consts.ENV_KEY_JWT_SECRET))
                .MustVerifySignature()
                .Decode<IDictionary<string, string>>(token);

            // Validate the audience.
            var aud = GetValue(payload, "aud");
            if (aud != WebApi.Consts.FEEDREADER_AUD && aud != WebApi.Consts.FEEDREADER_OLD_AUD)
            {
                throw new UnauthorizedAccessException();
            }

            // Get uuid which is required.
            var uid = GetValue(payload, "uid");
            if (string.IsNullOrWhiteSpace(uid))
            {
                throw new UnauthorizedAccessException();
            }

            // Token is valid.
            return new User
            {
                Id = uid
            };
        }

        private static async Task<User> AuthenticateMsTokenAsync(string token)
        {
            // Decode header to get kid
            var header = new JwtBuilder().DecodeHeader<IDictionary<string, string>>(token);
            var kid = GetValue(header, "kid");
            if (string.IsNullOrEmpty(kid))
            {
                throw new UnauthorizedAccessException();
            }

            // Get Microsoft Public Key.
            using (var http = new HttpClient())
            {
                var keys = JsonConvert.DeserializeObject<MicrosoftKeys>(await http.GetStringAsync(WebApi.Consts.MICROSOFT_PUBLIC_KEYS_URL));
                var key = keys.Keys.Where(k => k.Kid == kid).First();
                var cert = new X509Certificate2(new JwtBase64UrlEncoder().Decode(key.X5c.First()));

                // Validate the signature.
                var payload = new JwtBuilder()
                    .WithAlgorithm(new RS256Algorithm(cert))
                    .MustVerifySignature()
                    .Decode<IDictionary<string, string>>(token);

                // Validate the audience
                var aud = GetValue(payload, "aud");
                if (aud != WebApi.Consts.MICROSOFT_CLIENT_ID)
                {
                    throw new UnauthorizedAccessException();
                }

                // Validate the oid.
                var oid = GetValue(payload, "oid");
                if (string.IsNullOrEmpty(oid))
                {
                    throw new UnauthorizedAccessException();
                }

                // Return user.
                return new User()
                {
                    ThirdPartyId = $"ms:oid:{oid}",
                };
            }
        }

        private static async Task<User> AuthenticateGoogleTokenAsync(string token)
        {
            // Decode header to get kid
            var header = new JwtBuilder().DecodeHeader<IDictionary<string, string>>(token);
            var kid = GetValue(header, "kid");
            if (string.IsNullOrEmpty(kid))
            {
                throw new UnauthorizedAccessException();
            }

            using (var http = new HttpClient())
            {
                var keys = JsonConvert.DeserializeObject<IDictionary<string, string>>(await http.GetStringAsync(WebApi.Consts.GOOGLE_PUBLIC_KEYS_URL));
                var key = GetValue(keys, kid);
                var cert = new X509Certificate2(Encoding.UTF8.GetBytes(key));

                // Validate token.
                var payload = new JwtBuilder()
                    .WithAlgorithm(new RS256Algorithm(cert))
                    .MustVerifySignature()
                    .Decode<IDictionary<string, string>>(token);

                // Validate the audience
                var aud = GetValue(payload, "aud");
                if (aud != WebApi.Consts.GOOGLE_CLIENT_ID)
                {
                    throw new UnauthorizedAccessException();
                }

                // Validate the email address is verified
                var emailVerified = GetValue(payload, "email_verified");
                if (emailVerified != "true")
                {
                    throw new UnauthorizedAccessException();
                }

                // Validate the sub
                var sub = GetValue(payload, "sub");
                if (string.IsNullOrEmpty(sub))
                {
                    throw new UnauthorizedAccessException();
                }

                // Return user.
                return new User()
                {
                    ThirdPartyId = $"google:sub:{sub}",
                };
            }
        }

        private static async Task<User> AuthenticateGitHubAccessCodeAsync(string accessCode)
        {
            // Exchange access code to acess token.
            var uri = $"https://github.com/login/oauth/access_token?client_id={Environment.GetEnvironmentVariable(WebApi.Consts.ENV_KEY_GITHUB_CLIENT_ID)}&client_secret={Environment.GetEnvironmentVariable(WebApi.Consts.ENV_KEY_GITHUB_CLIENT_SECRET)}&code={accessCode}";
            var content = await new HttpClient().GetStringAsync(uri);
            var accessToken = HttpUtility.ParseQueryString(content)["access_token"];

            // Get user profile.
            var http = new HttpClient();
            http.DefaultRequestHeaders.Add("Authorization", $"token {accessToken}");
            http.DefaultRequestHeaders.Add("User-Agent", "feedreader.org");
            var json = await http.GetStringAsync($"https://api.github.com/user");
            var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            // Validate node_id
            var nodeId = GetValue(payload, "node_id");
            if (string.IsNullOrEmpty(nodeId))
            {
                throw new UnauthorizedAccessException();
            }

            // Return user.
            return new User()
            {
                ThirdPartyId = $"github:node_id:{nodeId}",
            };
        }

        private static async Task<User> AuthenticateFacebookAccessTokenAsync(string accessToken)
        {
            // Get user profile.
            var uri = $"https://graph.facebook.com/me?access_token={accessToken}&fields=name,email";
            var json = await new HttpClient().GetStringAsync(uri);
            var payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            // Validate id
            var id = GetValue(payload, "id");
            if (string.IsNullOrEmpty(id))
            {
                throw new UnauthorizedAccessException();
            }

            return new User()
            {
                ThirdPartyId = $"facebook:id:{id}",
            };
        }

        private static string GetValue(IDictionary<string, string> payload, string key)
        {
            string value;
            return payload.TryGetValue(key, out value) ? value : null;
        }

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
    }
}
