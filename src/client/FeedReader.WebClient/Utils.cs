using System.Web;

namespace FeedReader.WebClient
{
    public class Utils
    {
        public static int TimezoneOffset = 0;

        public static string SafeImageUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri) || uri.StartsWith("https://"))
            {
                return uri;
            }
            else
            {
                return string.Format("/_img_proxy/?url={0}", HttpUtility.UrlEncode(uri));
            }
        }
    }
}
