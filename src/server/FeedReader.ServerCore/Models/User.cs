using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

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
    }
}
