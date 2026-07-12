using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class CommentDisplayDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public short? Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserSurname { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsEdited { get; set; }
        public DateTime? EditedAt { get; set; }
        public Guid? ParentCommentId { get; set; }
        public List<CommentDisplayDto> Replies { get; set; } = new();
    }
}
