using System;
using System.Collections.Generic;
using System.Text;

namespace FeedReader.WebApi
{
    static class Consts
    {
        public const string FEEDREADER_PREFIX = "feedreader:";

        public const string FEEDREADER_UUID_PREFIX = FEEDREADER_PREFIX + "uuid:";

        public const string FEEDREADER_ISS = "https://www.feedreader.org";

        public const string FEEDREADER_AUD = "https://www.feedreader.org";

        public const string MICROSOFT_ISS = "https://login.microsoftonline.com/9188040d-6c67-4c5b-b112-36a304b66dad/v2.0";

        public const string MICROSOFT_CLIENT_ID = "dcaaa2ba-a614-4b8c-b78e-1fb39cb8899a";

        public const string MICROSOFT_PUBLIC_KEYS_URL = "https://login.microsoftonline.com/common/discovery/v2.0/keys";

        public const string GITHUB_PREFIX = "github:";

        public const string FACEBOOK_PREFIX = "facebook:";

        public const string GOOGLE_ISS = "https://accounts.google.com";

        public const string GOOGLE_CLIENT_ID = "2423499784-8btrctmdul3lrcjlg9uvaoa8clrtvc0f.apps.googleusercontent.com";

        public const string GOOGLE_PUBLIC_KEYS_URL = "https://www.googleapis.com/oauth2/v1/certs";

        public const string ENV_KEY_JWT_SECRET = "JwtSecret";

        public const string ENV_KEY_AZURE_STORAGE = "AzureWebJobsStorage";

        public const string ENV_KEY_GITHUB_CLIENT_ID = "GitHubClientId";

        public const string ENV_KEY_GITHUB_CLIENT_SECRET = "GitHubClientSecret";
    }
}
