using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.Models
{
    public class ShoppingCategory
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public Dictionary<string, string> Names { get; set; } = new();

        public virtual List<Ingredient> Ingredients { get; set; } = null!;

    }
}
