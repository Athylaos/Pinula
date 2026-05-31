using Pinula.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class CreateMealPlanDto
    {
        public required DateOnly Date { get; set; }
        public required MealType MealType { get; set; }
        public required Guid RecipeId { get; set; }
        public required Guid GroupId { get; set; }
        public int Servings { get; set; } = 1;
        public List<Guid> UsersId { get; set; }
    }
}
