using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Pinual.API.Models
{
    public class ShoppingCategory
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public Dictionary<string, string> Names { get; set; } = new();

        [JsonIgnore]
        public virtual List<Ingredient> Ingredients { get; set; } = null!;

    }
}
