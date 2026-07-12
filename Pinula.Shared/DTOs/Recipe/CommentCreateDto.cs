using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class CommentCreateDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid RecipeId { get; set; }
        public string? Text { get; set; }
        public short? Rating { get; set; }
        public Guid? ParentCommentId { get; set; }

    }
}
