using System.Linq.Expressions;
using Pinula.API.Models;
using Pinula.Shared.Constants;
using Pinula.Shared.DTOs;

namespace Pinula.API.Projections;

public class IngredientProjections
{
    public static Expression<Func<Ingredient, IngredientPreviewDto>> ToPreviewDto(string baseUrl, string lang)
    {
        return i => new IngredientPreviewDto
        {
            Id = i.Id,
            Name = (i.Names.ContainsKey(lang) && !string.IsNullOrEmpty(i.Names[lang]))
                ? i.Names[lang]
                : (i.Names.ContainsKey("en") && !string.IsNullOrEmpty(i.Names["en"])) ? i.Names["en"]: "Ingredient",
            ImageUrl = $"{baseUrl}{(string.IsNullOrWhiteSpace(i.ImageUrl) ? ImageConstants.DefaultIngredient : i.ImageUrl)}",
            DefaultUnit = new UnitPreviewDto
            {
                Id = i.DefaultUnit.Id,
                Name = (i.DefaultUnit.Names.ContainsKey(lang) && !string.IsNullOrEmpty(i.DefaultUnit.Names[lang]))
                    ? i.DefaultUnit.Names[lang]
                    : (i.DefaultUnit.Names.ContainsKey("en") && !string.IsNullOrEmpty(i.DefaultUnit.Names["en"])) ? i.DefaultUnit.Names["en"] : "Unit",
                Code = i.DefaultUnit.Code,
                ConversionFactor = 1
            },
            IngredientUnits = i.IngredientUnits.Select(iu => new UnitPreviewDto
            {
                Id = iu.UnitId,
                Name = iu.Unit.Names.ContainsKey(lang) && !string.IsNullOrEmpty(iu.Unit.Names[lang])
                    ? iu.Unit.Names[lang]
                    : iu.Unit.Names.ContainsKey("en") && !string.IsNullOrEmpty(iu.Unit.Names["en"]) ? iu.Unit.Names["en"] : "Unit",
                Code = iu.Unit.Code,
                ConversionFactor = iu.AmountInGrams
            }).ToList()
        };
    }
}