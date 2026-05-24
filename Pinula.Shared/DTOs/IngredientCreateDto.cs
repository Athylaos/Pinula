using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class IngredientCreateDto
    {
        public string Name { get; set; } = null!;
        public Guid DefaultUnitId { get; set; }
        public decimal Calories { get; set; }
        public decimal Proteins { get; set; }
        public decimal Fats { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Fiber { get; set; }

        public List<CreateIngredientUnitDto> AdditionalUnits { get; set; } = new();
    }

    public class CreateIngredientUnitDto
    {
        public Guid UnitId { get; set; }
        public decimal ToDefaultUnit { get; set; }
    }
}
