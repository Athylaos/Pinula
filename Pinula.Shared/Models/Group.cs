using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.Models
{
    public class Group
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Name { get; set; }
        public required string InviteCode { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
    }
}
