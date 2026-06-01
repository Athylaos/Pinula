using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class CategoryCreateDto
    {
        public string Name { get; set; }
        public short SortOrder { get; set; }
        public Guid? ParentCategory { get; set; }
    }
}
