using System;
using System.Collections.Generic;

namespace Pinula.Shared.Models;

public partial class RecipeStep
{
    public Guid Id { get; set; }

    public Guid RecipeId { get; set; }

    public required string Description { get; set; }

    public short StepNumber { get; set; }

    public virtual Recipe Recipe { get; set; } = null!;
}
