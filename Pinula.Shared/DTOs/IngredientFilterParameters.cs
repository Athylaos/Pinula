using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class IngredientFilterParameters
    {
        public string? SearchTerm { get; set; }
        public string? Barcode { get; set; }
        public int Amount { get; set; }
    }
}
