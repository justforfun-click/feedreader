using Microsoft.AspNetCore.Http;
using System;

namespace FeedReader.WebApi.AdminFunctions
{
    public class AdminFunctions
    {
        private static readonly string AdminKey = Environment.GetEnvironmentVariable(Consts.ENV_KEY_ADMIN_KEY);

        public static void VerifyAdminKey(HttpRequest req)
        {
            var adminKey = req.Headers["AdminKey"];
            if (adminKey != AdminKey)
            {
                throw new ExternalErrorExceptionNotFound();
            }
        }
    }
}
