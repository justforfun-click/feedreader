using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace FeedReader.ServerCore.Models
{
    [Index(nameof(UserId))]
    public class UserFavorites
    {
        [Key, StringLength(32)]
        public string UserId { get; set; }

        /// <summary>
        /// Md5 hash of favoriateItemId.
        /// </summary>
        [Key, StringLength(32)]
        public string FavoriteItemIdHash { get; set; }
    }
}
