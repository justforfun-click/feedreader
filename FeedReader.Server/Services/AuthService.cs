using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Collections.Generic;

namespace FeedReader.Server.Services
{
    public class AuthService
    {
        public Models.User AuthenticateToken(string jwtToken)
        {
            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                throw new UnauthorizedAccessException();
            }

            // Validate the signature.
            var payload = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(Environment.GetEnvironmentVariable(WebApi.Consts.ENV_KEY_JWT_SECRET))
                .MustVerifySignature()
                .Decode<IDictionary<string, string>>(jwtToken);

            // Validate the audience.
            var aud = GetValue(payload, "aud");
            if (aud != WebApi.Consts.FEEDREADER_AUD && aud != WebApi.Consts.FEEDREADER_OLD_AUD)
            {
                throw new UnauthorizedAccessException();
            }

            // Get uuid which is required.
            var uuid = GetValue(payload, "uuid");
            if (string.IsNullOrWhiteSpace(uuid))
            {
                throw new UnauthorizedAccessException();
            }

            // Token is valid.
            return new Models.User
            {
                Uuid = uuid
            };
        }

        private static string GetValue(IDictionary<string, string> payload, string key)
        {
            string value;
            return payload.TryGetValue(key, out value) ? value : null;
        }
    }
}
