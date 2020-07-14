using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace FeedReader.WebApi.Entities
{
    class UserEntity : TableEntity
    {
        public string Uuid { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string AvatarUrl { get; set; }

        public DateTime RegistrationTime { get; set; }
    }
}
