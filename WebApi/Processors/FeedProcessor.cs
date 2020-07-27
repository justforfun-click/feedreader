using FeedReader.Share.DataContracts;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace FeedReader.WebApi.Processors
{
    class FeedProcessor
    {
        private static readonly Regex HtmlTagRegex = new Regex("<.*?>");

        private static readonly Regex WhiteSpaceRegex = new Regex("\\s+");

        private static readonly Regex ImgRegex = new Regex("<img\\s.*?\\bsrc\\s*=\\s*[\"'](.*?)[\"'].*?>");

        public async Task<Feed> RefreshFeedAsync(string uri, bool noItems = false)
        {
            Feed feed = new Feed() { Uri = uri };
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(await new HttpClient().GetStringAsync(feed.Uri));

                var xmlns = new XmlNamespaceManager(xml.NameTable);
                xmlns.AddNamespace("media", "http://search.yahoo.com/mrss/");

                // Parse channel. As spec, every feed has only one channel.
                var channelNode = xml.SelectSingleNode("/rss/channel");
                feed.Name = channelNode["title"].InnerText;
                feed.WebsiteLink = channelNode["link"].InnerText;
                feed.Description = channelNode["description"].InnerText;
                feed.IconUri = channelNode.SelectSingleNode("/rss/channel/image")?["url"]?.InnerText;
                if (!noItems)
                {
                    foreach (XmlNode itemNode in channelNode.SelectNodes("/rss/channel/item"))
                    {
                        var item = ParseItem(itemNode, xmlns);
                        feed.Items.Add(item);
                    }
                }
            }
            catch (HttpRequestException)
            {
                feed.Error = "The feed uri is not reachable.";
            }
            catch (XmlException)
            {
                feed.Error = "The feed content is not valid.";
            }
            return feed;
        }

        private FeedItem ParseItem(XmlNode itemNode, XmlNamespaceManager xmlns)
        {
            FeedItem item = new FeedItem();
            item.Title = itemNode["title"].InnerText;
            item.PermentLink = itemNode["link"].InnerText;
            item.PubDate = DateTime.Parse(itemNode["pubDate"].InnerText);
            item.Content = itemNode["description"].InnerText;

            // Find media content, standard: media rss, https://www.rssboard.org/media-rss
            string imgUrl = null;
            var mediaContents = itemNode.SelectNodes("media:group/media:content", xmlns);
            if (mediaContents != null)
            {
                foreach (XmlNode mediaContent in mediaContents)
                {
                    var attributes = mediaContent.Attributes;
                    var medium = attributes["medium"]?.InnerText;
                    if (medium == "image")
                    {
                        // is default?
                        if (attributes["isDefault"]?.InnerText.Trim().ToLower() == "true")
                        {
                            imgUrl = attributes["url"].InnerText;
                            break;
                        }
                        else if (string.IsNullOrWhiteSpace(imgUrl))
                        {
                            imgUrl = attributes["url"].InnerText;
                        }
                    }
                }
            }

            // Can we find the picture in the content?
            if (string.IsNullOrWhiteSpace(imgUrl))
            {
                var match = ImgRegex.Match(item.Content);
                if (match.Success)
                {
                    var uri = match.Groups[1].Value;
                    if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                    {
                        item.TopicPictureUri = uri;
                    }
                }
            }
            else
            {
                item.TopicPictureUri = imgUrl;
            }

            // Get summary.
            item.Summary = HtmlTagRegex.Replace(item.Content, string.Empty);
            item.Summary = WhiteSpaceRegex.Replace(item.Summary, " ").Trim();
            if (item.Summary.Length > 500)
            {
                item.Summary = item.Summary.Substring(0, 500);
            }
            return item;
        }
    }
}
