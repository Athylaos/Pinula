using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class AdminCommentDto
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public string UserName { get; set; }
        public string UserSurname { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsApproved { get; set; }
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; }
    }
}
