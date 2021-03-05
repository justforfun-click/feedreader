using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeedReader.ServerCore.Models
{
    [Index(nameof(UserId))]
    public class UserFeedItem
    {
        [Key, StringLength(32)]
        public string UserId { get; set; }

        /// <summary>
        [Key, ForeignKey("FeedItem")]
        public string FeedItemId { get; set; }

        public bool IsFavorite { get; set; }

        #region Virtuals
        public virtual FeedItem FeedItem { get; set; }
        #endregion
    }
}
