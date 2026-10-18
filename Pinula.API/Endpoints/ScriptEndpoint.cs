using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.API.Models;
using Pinula.Shared.DTOs.DataGenerator;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Pinula.API.Endpoints;

public static class ScriptEndpoint
{
    public static void MapScriptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/script");
        
        group.MapPut("/batch-update-ingredients-ai", async (List<DGIngredientUpdateDto> dtos, PinulaDbContext db) =>
        {
            if (dtos == null || !dtos.Any())
            {
                return Results.BadRequest("No data provided.");
            }

            var ingredientIds = dtos.Select(d => d.Id).ToList();
            
            var ingredients = await db.Ingredients
                .Include(i => i.IngredientUnits)
                .Where(i => ingredientIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            var gUnit = await db.Units.FirstOrDefaultAsync(u => u.Code == "g");
            
            int updatedCount = 0;

            foreach (var dto in dtos)
            {
                if (!ingredients.TryGetValue(dto.Id, out var ingredient))
                {
                    continue;
                }
                
                if (dto.DefaultUnitId != Guid.Empty)
                    ingredient.DefaultUnitId = dto.DefaultUnitId;

                if (dto.ShoppingCategoryId != Guid.Empty)
                    ingredient.ShoppingCategoryId = dto.ShoppingCategoryId;
                
                ingredient.IsVegan = dto.IsVegan;
                ingredient.IsVegetarian = dto.IsVegetarian;
                ingredient.IsGlutenFree = dto.IsGlutenFree;
                ingredient.IsLactoseFree = dto.IsLactoseFree;

                if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
                    ingredient.ImageUrl = dto.ImageUrl;

                ingredient.NutriScore = dto.NutriScore;
                ingredient.NovaClassification = dto.NovaClassification;
                
                db.IngredientUnits.RemoveRange(ingredient.IngredientUnits);

                if (dto.AdditionalUnits != null && dto.AdditionalUnits.Any())
                {
                    foreach (var unitDto in dto.AdditionalUnits)
                    {
                        ingredient.IngredientUnits.Add(new IngredientUnit
                        {
                            IngredientId = ingredient.Id,
                            UnitId = unitDto.UnitId,
                            AmountInGrams = unitDto.AmountInGrams
                        });
                    }
                }

                if (!ingredient.IngredientUnits.Any(i => i.UnitId == gUnit.Id))
                {
                    ingredient.IngredientUnits.Add(new IngredientUnit
                    {
                        IngredientId = ingredient.Id,
                        UnitId = gUnit.Id,
                        AmountInGrams = 1
                    });
                }

                updatedCount++;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new { Message = $"Successfully updated {updatedCount} ingredients." });

        }).RequireAuthorization("AdminOnly");
        
        
        group.MapPost("/upload-ingredient-image", async ([FromForm] Guid ingredientId, IFormFile image, PinulaDbContext db, IWebHostEnvironment env) =>
        {
            if (image == null || image.Length == 0)
                return Results.BadRequest("No image file provided.");

            var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Id == ingredientId);
            if (ingredient == null)
                return Results.NotFound($"Ingredient with ID {ingredientId} not found.");

            var uploadFolder = Path.Combine(env.WebRootPath, "images", "ingredients");
            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            var fileName = $"{ingredientId}.webp";
            var filePath = Path.Combine(uploadFolder, fileName);

            try
            {
                using (var stream = image.OpenReadStream())
                using (var img = await Image.LoadAsync(stream))
                {
                    img.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(400, 400)
                    }));

                    await img.SaveAsWebpAsync(filePath, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder
                    {
                        Quality = 80
                    });
                }

                ingredient.ImageUrl = fileName;
                await db.SaveChangesAsync();

                return Results.Ok(new { Message = "Image uploaded successfully", ImageUrl = fileName });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error processing image: {ex.Message}");
            }

        }).RequireAuthorization("AdminOnly").DisableAntiforgery();
        
        
        // POST /script/fix-ingredient-image-urls
        group.MapPost("/fix-ingredient-image-urls", async (PinulaDbContext db, IWebHostEnvironment env) =>
        {
            var uploadFolder = Path.Combine(env.WebRootPath, "images", "ingredients");
            if (!Directory.Exists(uploadFolder))
                return Results.NotFound("Folder wwwroot/images/ingredients does not exist.");

            // Načteme všechny .webp soubory na disku
            var imageFiles = Directory.GetFiles(uploadFolder, "*.webp");
            int updatedCount = 0;

            foreach (var filePath in imageFiles)
            {
                var fileName = Path.GetFileName(filePath); // např. "be750f4d-a989-4052-b970-e59ad31c5ba0.webp"
                var idStr = Path.GetFileNameWithoutExtension(fileName);

                if (Guid.TryParse(idStr, out var ingredientId))
                {
                    var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Id == ingredientId);
            
                    // Pokud surovina existuje a má výchozí/prázdnou fotku, opravíme ji
                    if (ingredient != null && (string.IsNullOrEmpty(ingredient.ImageUrl) || ingredient.ImageUrl.Contains("default")))
                    {
                        ingredient.ImageUrl = fileName;
                        updatedCount++;
                    }
                }
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { Message = $"Successfully fixed image URLs for {updatedCount} ingredients." });

        }).RequireAuthorization("AdminOnly").DisableAntiforgery();
        
    }
    
}