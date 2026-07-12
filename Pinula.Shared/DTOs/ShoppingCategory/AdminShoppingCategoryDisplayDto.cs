using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class AdminShoppingCategoryDisplayDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public Dictionary<string, string> Names { get; set; } = new();
    }
}
