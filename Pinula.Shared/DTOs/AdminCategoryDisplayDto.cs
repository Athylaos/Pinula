using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class AdminCategoryDisplayDto
    {
        public Guid Id { get; set; }

        public Dictionary<string,string> Names { get; set; } = new();

        public required string PictureUrl { get; set; } = "default_category_picture.png";

        public short SortOrder { get; set; }

        public Guid? ParentCategoryId { get; set; }

        public int Level { get; set; }

        public virtual ICollection<AdminCategoryDisplayDto> ChildCategories { get; set; } = new List<AdminCategoryDisplayDto>();
        public virtual Category? ParentCategory { get; set; }
    }
}
