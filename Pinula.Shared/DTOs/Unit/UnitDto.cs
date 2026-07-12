using System;
using System.Collections.Generic;
using System.Text;

namespace Pinula.Shared.DTOs
{
    public class UnitDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public Dictionary<string, string> Names { get; set; } = new();
        public bool IsServingUnit { get; set; }
    }
}
