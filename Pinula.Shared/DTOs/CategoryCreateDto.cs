using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class CategoryCreateDto
    {
        public Dictionary<string, string> Names { get; set; } = new();
        public short SortOrder { get; set; }
        public Guid? ParentCategory { get; set; }
    }
}
