using Pinula.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class IngredientPreview
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string? ImageUrl { get; set; }

        public UnitPreviewDto DefaultUnit { get; set; }

        public UnitPreviewDto SelectedUnit { get; set; } = null!;

        public List<UnitPreviewDto> IngredientUnits { get; set; } = new List<UnitPreviewDto>();

    }
}
