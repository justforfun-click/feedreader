using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeedReader.ServerCore.Models
{
    [Index(nameof(UserId)), Index(nameof(FeedId))]
    public class UserFeed
    {
        [Key, ForeignKey("User")]
        public string UserId { get; set; }

        [Key, ForeignKey("Feed")]
        public string FeedId { get; set; }

        public string Group { get; set; }

        public DateTime LastReadedTimeInUtc { get; set; }

        #region Virtuals
        public virtual User User { get; set; }
        public virtual Feed Feed { get; set; }
        #endregion
    }
}
