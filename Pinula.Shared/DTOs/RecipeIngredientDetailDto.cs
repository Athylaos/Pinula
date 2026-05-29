using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class RecipeIngredientDetailDto
    {
        public decimal Quantity { get; set; }
        public string IngredientName { get; set; }
        public string UnitName { get; set; }

        public Guid IngredientId { get; set; }
        public Guid UnitId { get; set; }
    }
}
