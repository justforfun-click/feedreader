using HtmlAgilityPack;
using System;

namespace FeedReader.WebApi.Processors
{
    public class HtmlParser
    {
        public string ShortcurtIcon { get; private set; }

        private readonly string _baseUri;

        public HtmlParser(string htmlContent, string uri)
        {
            if (uri.EndsWith('/'))
            {
                _baseUri = uri;
            }
            else
            {
                var lastSlashPos = uri.LastIndexOf('/');
                if (lastSlashPos >= 0 && lastSlashPos != uri.IndexOf("//") + 1)
                {
                    _baseUri = uri.Remove(lastSlashPos + 1);
                }
                else
                {
                    _baseUri = uri + '/';
                }
            }
            Parse(htmlContent);
        }

        private void Parse(string htmlContent)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);
            if (htmlDoc.DocumentNode != null)
            {
                ParseShortcutIcon(htmlDoc.DocumentNode);
            }
        }

        private void ParseShortcutIcon(HtmlNode documentNode)
        {
            foreach (var possibleLinkNodePath in new string[] { "/html/head/link", "/html/body/link", "/html/link", "/link" })
            {
                var linkNodes = documentNode.SelectNodes(possibleLinkNodePath);
                if (linkNodes != null)
                {
                    foreach (var linkNode in linkNodes)
                    {
                        var href = linkNode.Attributes["href"]?.Value;
                        if (!string.IsNullOrWhiteSpace(href))
                        {
                            // https://en.wikipedia.org/wiki/Favicon
                            var rel = linkNode.Attributes["rel"]?.Value;
                            if (rel == "shortcut icon" || rel == "icon" || rel == "apple-touch-icon")
                            {
                                ShortcurtIcon = GetUri(href);
                                return;
                            }
                        }
                    }
                }
            }

            // <meta property="og:image"?
            foreach (var possibleMetaNodePath in new string[] { "/html/head/meta", "/html/body/meta", "/html/meta", "/meta" })
            {
                var metaNodes = documentNode.SelectNodes(possibleMetaNodePath);
                if (metaNodes != null)
                {
                    foreach (var metaNode in metaNodes)
                    {
                        var property = metaNode.Attributes["property"]?.Value;
                        if (property == "og:image")
                        {
                            var content = metaNode.Attributes["content"]?.Value;
                            if (!string.IsNullOrWhiteSpace(content))
                            {
                                ShortcurtIcon = GetUri(content);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private string GetUri(string raw)
        {
            if (Uri.IsWellFormedUriString(raw, UriKind.Absolute))
            {
                return raw;
            }
            else if (Uri.IsWellFormedUriString(raw, UriKind.Relative))
            {
                if (raw.StartsWith("//"))
                {
                    return new Uri(_baseUri).GetLeftPart(UriPartial.Scheme) + raw.Substring(2);
                }
                else if (raw.StartsWith('/'))
                {
                    return new Uri(_baseUri).GetLeftPart(UriPartial.Authority) + raw;
                }
                else
                {
                    return _baseUri + raw;
                }
            }
            return null;
        }
    }
}
