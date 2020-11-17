using Microsoft.Azure.Cosmos.Table;
using System;

namespace FeedReader.WebApi.Entities
{
    public class UserEntity : TableEntity
    {
        public string Uuid { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string AvatarUrl { get; set; }

        public DateTime RegistrationTime { get; set; }

        public string ReadedHashs { get; set; }

        public string StaredHashs { get; set; }
    }

    public class UserFeedItemStarsEntity : TableEntity
    {
        public string StaredFeedItems { get; set; }
    }
}
