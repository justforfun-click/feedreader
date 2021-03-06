namespace FeedReader.ClientCore.Models
{
    public class Feed
    {
        public string Uri { get; set; }

        public string OriginalUri { get; set; }

        public string IconUri { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string WebsiteLink { get; set; }

        public string Group { get; set; }

        public FeedItems Items { get; set; } = new FeedItems();

        public string Error { get; set; }

        public bool IsActive { get; set; }
    }
}
