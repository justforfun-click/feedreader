using System.Xml;

namespace FeedReader.Backend.Share.FeedParsers
{
    abstract public class XmlFeedParser : FeedParser
    {
        public XmlDocument FeedXml { get; }

        public XmlNamespaceManager FeedXmlNS { get; }

        public XmlFeedParser(XmlDocument xml)
        {
            FeedXml = xml;
            FeedXmlNS = new XmlNamespaceManager(xml.NameTable);
            FeedXmlNS.AddNamespace("media", "http://search.yahoo.com/mrss/");
        }

        public virtual string TryGetImageUrl(XmlNode node)
        {
            // Find media content, standard: media rss, https://www.rssboard.org/media-rss
            string imgUrl = null;
            var mediaContents = node.SelectNodes("media:group/media:content", FeedXmlNS);
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
            if (!string.IsNullOrWhiteSpace(imgUrl))
            {
                return imgUrl;
            }

            // have media:thumbnail?
            var thumbnail = node.SelectSingleNode("media:thumbnail", FeedXmlNS);
            if (thumbnail != null)
            {
                imgUrl = thumbnail.Attributes["url"].InnerText;
            }
            if (!string.IsNullOrWhiteSpace(imgUrl))
            {
                return imgUrl;
            }

            // have media:group/media:thumbnail?
            thumbnail = node.SelectSingleNode("media:group/media:thumbnail", FeedXmlNS);
            if (thumbnail != null)
            {
                imgUrl = thumbnail.Attributes["url"].InnerText;
            }
            if (!string.IsNullOrWhiteSpace(imgUrl))
            {
                return imgUrl;
            }

            // Not found.
            return null;
        }
    }
}
