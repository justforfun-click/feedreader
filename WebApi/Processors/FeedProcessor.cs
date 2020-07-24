using FeedReader.Share.DataContracts;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace FeedReader.WebApi.Processors
{
    class FeedProcessor
    {
        public async Task<Feed> RefreshFeed(string uri)
        {
            Feed feed = new Feed();
            var content = await new HttpClient().GetStringAsync(uri);
            using (var xml = XmlReader.Create(new StringReader(content)))
            {
                var reader = new RssFeedReader(xml);
                while (await reader.Read())
                {
                    if (reader.ElementType == SyndicationElementType.Item)
                    {
                        var feedItem = new FeedItem();
                        var feedContent = await reader.ReadContent();
                        foreach (var field in feedContent.Fields)
                        {
                            switch (field.Name)
                            {
                                case "title":
                                    feedItem.Title = field.Value;
                                    break;

                                case "link":
                                    feedItem.PermentLink = field.Value;
                                    break;

                                case "description":
                                    feedItem.Content = field.Value;
                                    break;

                                case "pubDate":
                                    feedItem.PubDate = DateTime.Parse(field.Value);
                                    break;
                            }
                        }
                        feed.Items.Add(feedItem);
                    }
                }
            }
            return feed;
        }
    }
}
