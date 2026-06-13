using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class CommentPreview
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public short? Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; }
        public string UserSurname { get; set; }
        public bool IsApproved { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public Guid? ParentCommentId { get; set; }
        public virtual ICollection<CommentPreview> Replies { get; set; } = new List<CommentPreview>();
    }
}
