using Pinula.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.Models
{
    public class MealPlan
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid GroupId { get; set; }
        public Guid RecipeId { get; set; }
        public DateTime Date { get; set; }
        public MealType MealType { get; set; }
        public int Servings { get; set; } = 1;


        public Group? Group { get; set; }
        public Recipe? Recipe { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
