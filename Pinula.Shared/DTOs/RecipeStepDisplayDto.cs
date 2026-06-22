using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class RecipeStepDisplayDto
    {
        public Guid Id { get; set; }

        public Guid RecipeId { get; set; }

        public string Description { get; set; } = string.Empty;

        public short StepNumber { get; set; }
    }
}
