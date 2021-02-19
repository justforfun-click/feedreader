using System;
using System.ComponentModel.DataAnnotations;

namespace FeedReader.ServerCore.Models
{
    public class Feed
    {
        [Key, StringLength(64)]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        [Required]
        public string Uri { get; set; }
        public string IconUri { get; set; }
        public string WebSiteUri { get; set; }
        [Required]
        public DateTime RegistrationTimeInUtc { get; set; }
        [Required]
        public DateTime LastUpdateTimeInUtc { get; set; }
    }
}
