using FeedReader.Backend.Share.Entities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace FeedReader.Backend.Share.FeedParsers
{
    public abstract class FeedParser
    {
        private static readonly Regex HtmlTagRegex = new Regex("<.*?>");

        private static readonly Regex WhiteSpaceRegex = new Regex("\\s+");

        private static readonly Regex ImgRegex = new Regex("<img\\s.*?\\bsrc\\s*=\\s*[\"'](.*?)[\"'].*?>");

        private static readonly Regex VideoRegex = new Regex("<video\\s.*?\\bposter\\s*=\\s*[\"'](.*?)[\"'].*?>");

        public abstract FeedInfoEntity ParseFeedInfo();

        public abstract List<FeedItemEntity> ParseFeedItems();

        protected string TryGetImageUrl(string content)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                var match = ImgRegex.Match(content);
                if (match.Success)
                {
                    var uri = match.Groups[1].Value;
                    if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                    {
                        return uri;
                    }
                }

                match = VideoRegex.Match(content);
                if (match.Success)
                {
                    var uri = match.Groups[1].Value;
                    if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                    {
                        return uri;
                    }
                }
            }
            return null;
        }

        protected string GetSummary(string content)
        {
            content = HtmlTagRegex.Replace(content, string.Empty);
            content = WhiteSpaceRegex.Replace(content, " ").Trim();
            if (content.Length > 500)
            {
                content = content.Substring(0, 500);
            }
            return content;
        }

        public static FeedParser Create(string content)
        {
            var xml = new XmlDocument();
            xml.LoadXml(content);
            if (xml.DocumentElement?.Name == "feed")
            {
                return new AtomFeedParser(xml);
            }
            else
            {
                return new RssFeedParser(xml);
            }
        }
    }
}
