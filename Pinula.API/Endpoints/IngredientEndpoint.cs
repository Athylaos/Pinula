using DeepL;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using System.Globalization;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace Pinula.API.Endpoints
{
    public static class IngredientEndpoint
    {
        public static void MapIngredientEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/ingredients");

            //---------------------------------------------------------------Get filtered previews
            group.MapGet("/getFilteredPreviews", async ([AsParameters] IngredientFilterParameters filter, PinulaDbContext db) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                filter.Amount = (filter.Amount > 0) ? filter.Amount : 20;

                var query = db.Ingredients
                    .AsNoTracking()
                    .Include(i => i.DefaultUnit)
                    .Include(i => i.IngredientUnits)
                    .ThenInclude(iu => iu.Unit)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(filter.Barcode))
                {
                    query = query.Where(i => i.Barcode == filter.Barcode);
                }
                else if (string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    return Results.Ok(new List<IngredientPreviewDto>());
                }

                var rawIngredients = await query.ToListAsync();

                if (string.IsNullOrWhiteSpace(filter.Barcode) && !string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    string term = filter.SearchTerm.ToLower();

                    rawIngredients = rawIngredients.Where(i =>
                        (i.Names.TryGetValue(languageCode, out var nameCs) && nameCs.ToLower().Contains(term)) ||
                        (i.Names.TryGetValue("en", out var nameEn) && nameEn.ToLower().Contains(term))
                    ).ToList();
                }

                var results = rawIngredients
                    .Take(filter.Amount)
                    .Select(i => new IngredientPreviewDto
                    {
                        Id = i.Id,
                        Name = i.Names.GetValueOrDefault(languageCode) ?? i.Names.GetValueOrDefault("en") ?? "Ingredient",
                        ImageUrl = i.ImageUrl,
                        DefaultUnit = new UnitPreviewDto
                        {
                            Id = i.DefaultUnit.Id,
                            Name = i.DefaultUnit.Names.GetValueOrDefault(languageCode) ?? i.DefaultUnit.Names.GetValueOrDefault("en") ?? "Unit",
                            Code = i.DefaultUnit.Code,
                            ConversionFactor = 1
                        },
                        IngredientUnits = i.IngredientUnits.Select(iu => new UnitPreviewDto
                        {
                            Id = iu.UnitId,
                            Name = iu.Unit.Names.GetValueOrDefault(languageCode) ?? iu.Unit.Names.GetValueOrDefault("en") ?? "Unit",
                            Code = iu.Unit.Code,
                            ConversionFactor = iu.AmountInGrams
                        }).ToList()
                    }).ToList();

                return Results.Ok(results);
            });


            //---------------------------------------------------------------Create ingredient
            group.MapPost("/create", async (IngredientCreateDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                try
                {
                    Guid? finalTypeId = null;

                    if (dto.CategoryTags != null && dto.CategoryTags.Any())
                    {
                        var existingTCategory = await db.ShoppingCategories
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => dto.CategoryTags.Contains(t.Code.ToLower()));

                        if (existingTCategory != null)
                        {
                            finalTypeId = existingTCategory.Id;
                        }
                    }

                    if (finalTypeId == null)
                    {
                        var uncategorizedType = await db.ShoppingCategories
                            .AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Code == "Uncategorized");

                        finalTypeId = uncategorizedType?.Id;
                    }

                    var ingredient = new Ingredient
                    {
                        Id = Guid.NewGuid(),
                        Names = dto.Names,
                        Barcode = dto.Barcode,
                        ImageUrl = dto.ImageUrl,
                        ShoppingCategoryId = finalTypeId??Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        DefaultUnitId = dto.DefaultUnitId,

                        Calories = dto.Calories,
                        Proteins = dto.Proteins,
                        Fats = dto.Fats,
                        SaturatedFats = dto.SaturatedFats,
                        Carbohydrates = dto.Carbohydrates,
                        Sugars = dto.Sugars,
                        Fiber = dto.Fiber,
                        Salt = dto.Salt,

                        NutriScore = dto.NutriScore,
                        NovaClassification = dto.NovaClassification,
                        IsVegan = dto.IsVegan,
                        IsVegetarian = dto.IsVegetarian,
                        IsGlutenFree = dto.IsGlutenFree,
                        IsLactoseFree = dto.IsLactoseFree
                    };

                    if (dto.AdditionalUnits != null)
                    {
                        foreach (var iu in dto.AdditionalUnits)
                        {
                            ingredient.IngredientUnits.Add(new IngredientUnit
                            {
                                IngredientId = ingredient.Id,
                                UnitId = iu.UnitId,
                                AmountInGrams = iu.ToDefaultUnit
                            });
                        }
                    }

                    db.Ingredients.Add(ingredient);
                    await db.SaveChangesAsync();

                    return Results.Ok("Ingredient added successfully");
                }
                catch (Exception ex)
                {
                    return Results.Problem($"An error occurred while saving the ingredient. Ex: {ex.Message}");
                }

            }).RequireAuthorization();



        }

    }
}
