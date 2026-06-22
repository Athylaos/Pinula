using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Pinula.Shared.DTOs
{
    public class CategoryDisplayDto
    {
        public Guid Id { get; set; }

        public required string Name { get; set; } = string.Empty;

        public required string PictureUrl { get; set; } = "default_category_picture.png";

        public short SortOrder { get; set; }

        public Guid? ParentCategoryId { get; set; }

        public virtual ICollection<CategoryDisplayDto> ChildCategories { get; set; } = new List<CategoryDisplayDto>();
        public virtual Category? ParentCategory { get; set; }
    }
}
