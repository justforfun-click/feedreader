using System.Web;

namespace FeedReader.WebClient
{
    public class Utils
    {
        public static string SafeImageUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri) || uri.StartsWith("https://"))
            {
                return uri;
            }
            else
            {
                return string.Format("https://proxy.feedreader.org?url={0}", HttpUtility.UrlEncode(uri));
            }
        }
    }
}
