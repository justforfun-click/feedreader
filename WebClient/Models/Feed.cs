using System;

namespace FeedReader.WebClient.Models
{
    public class Feed
    {
        public string Name { get; set; }

        public string Uri { get; set; }

        public string Group { get; set; }

        public string IconUri
        {
            get
            {
                var uri = new Uri(Uri);
                return $"https://www.google.com/s2/favicons?domain={uri.Host}";
            }
        }
    }
}