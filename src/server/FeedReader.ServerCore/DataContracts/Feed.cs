﻿using System.Collections.Generic;

namespace FeedReader.Share.DataContracts
{
    public class Feed
    {
        public string Uri { get; set; }

        public string OriginalUri { get; set; }

        public string IconUri { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string WebsiteLink { get; set; }

        public string Group { get; set;  }

        public List<FeedItem> Items { get; set; } = new List<FeedItem>();

        public string Error { get; set; }
    }
}
