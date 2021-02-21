using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeedReader.ServerCore.Models
{
    [Index(nameof(UserId))]
    public class UserFavorite
    {
        [Key, StringLength(32)]
        public string UserId { get; set; }

        /// <summary>
        /// Md5 hash of favoriateItemId.
        /// </summary>
        [Key, StringLength(32)]
        public string FavoriteItemIdHash { get; set; }

        [ForeignKey("FeedItem")]
        public string FeedItemId { get; set; }

        #region Virtuals
        public virtual FeedItem FeedItem { get; set; }
        #endregion
    }
}
