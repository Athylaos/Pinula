namespace Pinula.WASM.Services;

public static class HelperService
{
    public static decimal FormatSmart(decimal quantity, string? unitCode = null)
    {
        if (quantity <= 0) return 0m;

        var code = unitCode?.ToLowerInvariant().Trim();
        
        if (code == "g" || code == "ml")
        {
            if (quantity >= 100)
            {
                return Math.Round(quantity / 5m, MidpointRounding.AwayFromZero) * 5m;
            }

            if (quantity >= 10)
            {
                return Math.Round(quantity, MidpointRounding.AwayFromZero);
            }
            
            return Math.Round(quantity, 1, MidpointRounding.AwayFromZero);
        }
        
        if (code == "ks" || code == "pcs" || code == "piece" || string.IsNullOrEmpty(code))
        {
            if (quantity >= 10)
            {
                return Math.Round(quantity, MidpointRounding.AwayFromZero);
            }
            
            return Math.Round(quantity * 4m, MidpointRounding.AwayFromZero) / 4m;
        }
        
        if (quantity >= 10)
        {
            return Math.Round(quantity, 1, MidpointRounding.AwayFromZero);
        }
    
        return Math.Round(quantity, 2, MidpointRounding.AwayFromZero);
    }
}
