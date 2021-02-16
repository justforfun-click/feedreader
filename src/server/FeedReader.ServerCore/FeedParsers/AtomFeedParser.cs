using FeedReader.Backend.Share.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

namespace FeedReader.Backend.Share.FeedParsers
{
    public class AtomFeedParser : XmlFeedParser
    {
        public AtomFeedParser(XmlDocument xml)
            : base(xml)
        {
            FeedXmlNS.AddNamespace("ns", "http://www.w3.org/2005/Atom");
        }

        public override FeedInfoEntity ParseFeedInfo()
        {
            var feed = new FeedInfoEntity();
            var feedNode = FeedXml.SelectSingleNode("/ns:feed", FeedXmlNS);
            feed.Name = feedNode["title"].InnerText;
            feed.WebsiteLink = GetLinkRef(feedNode);
            feed.Description = feedNode["subtitle"]?.InnerText;

            // If there is not website link, use the first author link.
            if (string.IsNullOrWhiteSpace(feed.WebsiteLink))
            {
                feed.WebsiteLink = feedNode.SelectSingleNode("/ns:feed/ns:author/ns:uri", FeedXmlNS)?.InnerText;
            }

            // TODO: Parse icon.
            return feed;
        }

        public override List<FeedItemEntity> ParseFeedItems()
        {
            var feedItems = new List<FeedItemEntity>();
            foreach (XmlNode itemNode in FeedXml.SelectNodes("/ns:feed/ns:entry", FeedXmlNS))
            {
                feedItems.Add(ParseItem(itemNode));
            }
            return feedItems;
        }

        private FeedItemEntity ParseItem(XmlNode itemNode)
        {
            var item = new FeedItemEntity();
            item.Title = itemNode["title"].InnerText;
            item.PermentLink = GetLinkRef(itemNode);
            item.Guid = itemNode["id"]?.InnerText;

            // Get content and summary.
            item.Summary = itemNode["summary"]?.InnerText;
            item.Content = itemNode["content"]?.InnerText;
            if (string.IsNullOrWhiteSpace(item.Content))
            {
                item.Content = item.Summary;
            }

            // Normalize summary.
            if (string.IsNullOrWhiteSpace(item.Summary))
            {
                item.Summary = item.Content;
            }
            if (!string.IsNullOrWhiteSpace(item.Summary))
            {
                item.Summary = GetSummary(item.Summary);
            }

            // Get pubdate.
            // If we have updated node, use updated node as pubdate.
            if (!string.IsNullOrWhiteSpace(itemNode["updated"]?.InnerText))
            {
                item.PubDate = DateTime.Parse(itemNode["updated"]?.InnerText);
            }
            else
            {
                item.PubDate = DateTime.Parse(itemNode["published"]?.InnerText);
            }

            // Get Image url
            string imgUrl = TryGetImageUrl(itemNode);
            if (string.IsNullOrWhiteSpace(imgUrl))
            {
                imgUrl = TryGetImageUrl(item.Content);
            }

            // Save topic image.
            item.TopicPictureUri = imgUrl;

            // All done.
            return item;
        }

        private string GetLinkRef(XmlNode node, string expectedRel = null)
        {
            expectedRel = expectedRel ?? "alternate";
            var links = node.SelectNodes("ns:link", FeedXmlNS);
            if (links != null)
            {
                foreach (XmlNode link in links)
                {
                    var attributes = link.Attributes;
                    var rel = attributes["rel"]?.InnerText;
                    if (rel == expectedRel || rel == null && expectedRel == "alternate")
                    {
                        return attributes["href"].InnerText.Trim();
                    }
                }
            }
            return null;
        }

    }
}
