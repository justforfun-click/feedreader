using System.Text.RegularExpressions;

namespace FeedReader.ServerCore
{
    public class Utils : FeedReader.Share.Utils
    {
        private static Regex HtmlTagRegex = new Regex("<.*?>");

        public static string RemoveHtmlTag(string content)
        {
            return HtmlTagRegex.Replace(content, string.Empty);
        }
    }
}
