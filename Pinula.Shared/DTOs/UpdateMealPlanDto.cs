using Pinula.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class UpdateMealPlanDto
    {
        public Guid Id { get; set; }
        public required DateTime Date { get; set; }
        public required MealType MealType { get; set; }
        public int Servings { get; set; }
        public required List<Guid> UsersIds { get; set; }
    }
}
