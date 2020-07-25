using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FeedReader.WebApi.Datas;
using JWT.Builder;
using JWT.Algorithms;
using JWT;
using System.Text;

namespace FeedReader.WebApi.Extensions
{
    [Binding]
    class AuthenticationAttribute : Attribute
    {
        public bool AllowThirdPartyToken { get; set; }
    }

    class AuthenticationValueProvider : IValueProvider
    {
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

        private readonly HttpRequest _req;

        private readonly AuthenticationAttribute _attr;

        public Type Type => typeof(User);

        public AuthenticationValueProvider(HttpRequest req, AuthenticationAttribute attr)
        {
            _req = req;
            _attr = attr;
        }

        public Task<object> GetValueAsync()
        {
            return HttpFilter.RunAsync(_req, async () =>
            {
                try
                {
                    // Get token from header.
                    var token = _req.Headers["authentication"];
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        token = _req.Query["authentication"];
                    }

                    // Decode it.
                    var payload = new JwtBuilder().DoNotVerifySignature().Decode<IDictionary<string, string>>(token);
                    var iss = GetRequiredField(payload, "iss");

                    // Authenticate token based on issuer.
                    if (iss == Consts.FEEDREADER_ISS)
                    {
                        return AuthenticateFeedReaderToken(token);
                    }
                    else if (_attr.AllowThirdPartyToken)
                    {
                        switch (iss)
                        {
                            case Consts.MICROSOFT_ISS:
                                return await AuthenticateMsTokenAsync(token);

                            case Consts.GOOGLE_ISS:
                                return await AuthenticateGoogleTokenAsync(token);
                        }
                    }
                }
                catch
                {
                }
                return new UnauthorizedResult();
            });
        }

        public string ToInvokeString()
        {
            return null;
        }

        private static User AuthenticateFeedReaderToken(string token)
        {
            // Validate the signature.
            var payload = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Environment.GetEnvironmentVariable(Consts.ENV_KEY_JWT_SECRET))
                .MustVerifySignature()
                .Decode<IDictionary<string, string>>(token);

            // Validate the audience
            if (GetRequiredField(payload, "aud") != Consts.FEEDREADER_AUD)
            {
                throw new UnauthorizedAccessException();
            }

            // Return user
            return new User()
            {
                Uuid = GetRequiredField(payload, "uuid"),
            };
        }

        private static async Task<User> AuthenticateMsTokenAsync(string token)
        {
            // Decode header to get kid
            var header = new JwtBuilder().DecodeHeader<IDictionary<string, string>>(token);
            var kid = GetRequiredField(header, "kid");

            // Get Microsoft Public Key.
            using (var http = new HttpClient())
            {
                var keys = JsonConvert.DeserializeObject<MicrosoftKeys>(await http.GetStringAsync(Consts.MICROSOFT_PUBLIC_KEYS_URL));
                var key = keys.Keys.Where(k => k.Kid == kid).First();
                var cert = new X509Certificate2(new JwtBase64UrlEncoder().Decode(key.X5c.First()));

                // Validate the signature.
                var payload = new JwtBuilder()
                    .WithAlgorithm(new RS256Algorithm(cert))
                    .MustVerifySignature()
                    .Decode<IDictionary<string, string>>(token);

                // Validate the audience
                if (GetRequiredField(payload, "aud") != Consts.MICROSOFT_CLIENT_ID)
                {
                    throw new UnauthorizedAccessException();
                }

                // Return user.
                return new User()
                {
                    Uuid = "ms:oid:" + GetRequiredField(payload, "oid"),
                    Name = GetRequiredField(payload, "name"),
                    Email = GetRequiredField(payload, "email").ToLower(),
                };
            }
        }

        private static async Task<User> AuthenticateGoogleTokenAsync(string token)
        {
            // Decode header to get kid
            var header = new JwtBuilder().DecodeHeader<IDictionary<string, string>>(token);
            var kid = GetRequiredField(header, "kid");

            using (var http = new HttpClient())
            {
                var keys = JsonConvert.DeserializeObject<IDictionary<string, string>>(await http.GetStringAsync(Consts.GOOGLE_PUBLIC_KEYS_URL));
                var key = GetRequiredField(keys, kid);
                var cert = new X509Certificate2(Encoding.UTF8.GetBytes(key));

                // Validate token.
                var payload = new JwtBuilder()
                    .WithAlgorithm(new RS256Algorithm(cert))
                    .MustVerifySignature()
                    .Decode<IDictionary<string, string>>(token);

                // Validate the audience
                if (GetRequiredField(payload, "iss") != Consts.GOOGLE_ISS)
                {
                    throw new ArgumentException("The iss in token is not Google.");
                }

                // Validate the audience
                if (GetRequiredField(payload, "aud") != Consts.GOOGLE_CLIENT_ID)
                {
                    throw new ArgumentException("The aud in token is not feedreader.org.");
                }

                // Validate the email address is verified
                if (GetRequiredField(payload, "email_verified") != "true")
                {
                    throw new ArgumentException("The email address is not verified.");
                }

                // Return user.
                return new User()
                {
                    Uuid = "google:sub:" + GetRequiredField(payload, "sub"),
                    Name = GetRequiredField(payload, "name"),
                    Email = GetRequiredField(payload, "email").ToLower(),
                    AvatarUrl = GetOptionalField(payload, "picture"),
                };
            }
        }

    
        private static string GetRequiredField(IDictionary<string, string> payload, string key)
        {
            string value;
            if (!payload.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
            {
                throw new KeyNotFoundException($"The '{key}' can't be found in the dictionary.");
            }
            return value;
        }

        private static string GetOptionalField(IDictionary<string, string> payload, string key)
        {
            string value;
            if (payload.TryGetValue(key, out value))
            {
                value = value.Trim();
            }
            return value ?? string.Empty;
        }
    }

    class AuthenticationBinding : IBinding
    {
        private readonly AuthenticationAttribute _attr;

        public bool FromAttribute => false;

        public AuthenticationBinding(AuthenticationAttribute attr)
        {
            _attr = attr;
        }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
            throw new NotImplementedException();
        }

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            var req = context.BindingData.First(pair => pair.Value is HttpRequest).Value as HttpRequest;
            return Task.FromResult<IValueProvider>(new AuthenticationValueProvider(req, _attr));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor();
        }
    }

    class AuthenticationBindingProvider : IBindingProvider
    {
        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            var attr = context.Parameter.GetCustomAttribute<AuthenticationAttribute>();
            return Task.FromResult<IBinding>(new AuthenticationBinding(attr));
        }
    }

    class AuthenticationExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            context.AddBindingRule<AuthenticationAttribute>().Bind(new AuthenticationBindingProvider());
        }
    }
}
