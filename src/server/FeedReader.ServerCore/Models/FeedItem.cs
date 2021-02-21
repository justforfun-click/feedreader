using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeedReader.ServerCore.Models
{
    public class FeedItem
    {
        [Key, StringLength(64)]
        public string Id { get; set; }
        [ForeignKey("Feed")]
        public string FeedId { get; set; }
        [Required]
        public string Uri { get; set; }
        public DateTime PublishTimeInUtc { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string Title { get; set; }
        public string TopicPictureUri { get; set; }

        #region Virtuals
        public virtual Feed Feed { get; set; }
        #endregion
    }
}
