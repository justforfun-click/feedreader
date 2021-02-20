using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeedReader.ServerCore.Models
{
    [Index(nameof(ThirdPartyId), IsUnique = true)]
    public class User
    {
        [Key, StringLength(32)]
        public string Id { get; set; }
        public string ThirdPartyId { get; set; }
        [Required]
        public DateTime RegistrationTimeInUtc { get; set; }
        [Required]
        public DateTime LastActiveTimeInUtc { get; set; }

        public virtual ICollection<UserFeed> Feeds { get; set; }

        [NotMapped]
        public string Token { get; set; }
    }
}
