using DeepL;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.API.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

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
            group.MapPost("/create", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db, IWebHostEnvironment env) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var userId = user.GetUserId();
                
                var form = await request.ReadFormAsync();

                var dtoStr = form["ingredientData"];
                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Missing ingredient data.");

                var dto = JsonSerializer.Deserialize<IngredientCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Invalid recipe data.");

                string finalPhotoUrl = "default_ingredient_picture.png";
                var file = form.Files.GetFile("image");

                if (file is { Length: > 0 })
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                        return Results.BadRequest("Unsupported image format.");

                    var uploadFolder = Path.Combine(env.WebRootPath, "images", "ingredients");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    try
                    {
                        using (var image = await Image.LoadAsync(file.OpenReadStream()))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Mode = ResizeMode.Crop,
                                Size = new Size(1200, 1200)
                            }));

                            await image.SaveAsJpegAsync(filePath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                            {
                                Quality = 80
                            });
                        }

                        finalPhotoUrl = fileName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Image Processing Error: {ex.Message}");
                    }
                }

                if(finalPhotoUrl == "default_ingredient_picture.png" && !string.IsNullOrWhiteSpace(dto.ImageUrl)) finalPhotoUrl = dto.ImageUrl;

                try
                {
                    ShoppingCategory? ct = null;
                    if(dto.CategoryTagId is not null)
                    {
                        ct = await db.ShoppingCategories.AsNoTracking().FirstOrDefaultAsync(ct => ct.Id == dto.CategoryTagId);
                    }
                    else
                    {
                        foreach(var cs in dto.CategoryTags)
                        {
                            var cts = await db.ShoppingCategories.AsNoTracking().FirstOrDefaultAsync(sc => sc.Code == cs);
                            if(cts is not null)
                            {
                                ct = cts;
                                break;
                            }
                        }
                    }

                    if (ct is null)
                    {
                        ct = await db.ShoppingCategories
                             .AsNoTracking()
                             .FirstOrDefaultAsync(t => t.Code == "other");
                    }


                    var ingredient = new Ingredient
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Names = dto.Names,
                        Barcode = dto.Barcode,
                        ImageUrl = finalPhotoUrl,
                        ShoppingCategoryId = ct.Id,
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

            //---------------------------------------------------------------Get filtered previews
            group.MapGet("/getAdminPreviews", async (int amount, int skip, HttpRequest request, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/ingredients/";
                var defaultImage = "default_ingredient.png";


                var list = await db.Ingredients.AsNoTracking().Select(i => new AdminIngredientPreviewDto
                {
                    Id = i.Id,
                    IsDeleted = i.IsDeleted,
                    IsApproved = i.IsApproved,
                    IngredientCreated = i.IngredientCreated,
                    Checked = i.Checked,
                    Names = i.Names,
                    ImageUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(i.ImageUrl) ? defaultImage : i.ImageUrl)}",
                }).ToListAsync();


                return Results.Ok(list);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Get ingredient detail admin
            group.MapGet("/getAdmin/{ingredientId:guid}", async (Guid ingredientId, HttpRequest request, PinulaDbContext db) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/ingredients/";
                var defaultImage = "default_ingredient.png";

                var ingredientDb = await db.Ingredients.AsNoTrackingWithIdentityResolution().Include(i => i.DefaultUnit).Include(i => i.ShoppingCategory).Include(i => i.IngredientUnits).ThenInclude(iu => iu.Unit).Include(i => i.BaseIngredient).FirstOrDefaultAsync(i => i.Id == ingredientId);
                if (ingredientDb is null) return Results.NotFound();

                var ingredientDto = ingredientDb.Adapt<AdminIngredientDisplayDto>();

                ingredientDto.DefaultUnit = new() { Code = ingredientDb.DefaultUnit.Code, Id = ingredientDb.DefaultUnit.Id, Name = ingredientDb.DefaultUnit.Names[languageCode] };
                ingredientDto.ShoppingCategory = ingredientDb.ShoppingCategory.Adapt<AdminShoppingCategoryDisplayDto>();
                ingredientDto.AdditionalUnits = ingredientDb.IngredientUnits.Select(i => new IngredientUnitPreviewDto {
                    AmountInGrams = i.AmountInGrams,
                    Unit = new() { Code = i.Unit.Code, Id = i.Unit.Id, ConversionFactor = i.AmountInGrams, Name = i.Unit.Names[languageCode] },
                }).ToList();
                ingredientDto.ImageUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(ingredientDb.ImageUrl) ? defaultImage : ingredientDb.ImageUrl)}";

                return Results.Ok(ingredientDto);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Delete ingredient admin
            group.MapDelete("/deleteAdmin/{ingredientId:guid}", async (Guid ingredientId, PinulaDbContext db) =>
            {

                var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Id == ingredientId);
                if (ingredient is null) return Results.NotFound(new GeneralResponse() { Successful = false, StatusCode = (int)HttpStatusCode.NotFound, Message = $"Ingredient with id:{ingredientId} does not exist." });

                if(await db.RecipeIngredients.AnyAsync(ri => ri.IngredientId == ingredientId)){
                    ingredient.IsDeleted = true;
                    await db.SaveChangesAsync();
                    return Results.Ok(new GeneralResponse() { Successful = true, StatusCode = (int)HttpStatusCode.OK, Message = $"Ingredient with id:{ingredientId} is used in recipes, successfuly SOFT deleted." });
                }
                else
                {
                    db.Remove(ingredient);
                    await db.SaveChangesAsync();
                    return Results.Ok(new GeneralResponse() { Successful = true, StatusCode = (int)HttpStatusCode.OK, Message = $"Ingredient with id:{ingredientId} successfuly permanently deleted." });
                }          
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Update ingredient admin
            group.MapPut("/updateAdmin", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db, IWebHostEnvironment env) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var userId = user.GetUserId();

                var form = await request.ReadFormAsync();

                var dtoStr = form["ingredientData"];
                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Missing ingredient data.");

                var dto = JsonSerializer.Deserialize<IngredientCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Invalid ingredient data.");

                var ingredient = await db.Ingredients.Include(i => i.IngredientUnits).FirstOrDefaultAsync(i => i.Id == dto.Id);
                if (ingredient == null) return Results.NotFound($"Ingredient with id:{dto.Id} does not exist.");

                string finalPhotoUrl = ingredient.ImageUrl;
                if(finalPhotoUrl is not null)
                {
                    if (finalPhotoUrl.Contains("/ingredients/"))
                    {
                        const string target = "/ingredients/";
                        int index = finalPhotoUrl.IndexOf(target);

                        if (index == -1)
                        {

                        }
                        else
                        {
                            int startIndex = index + target.Length;
                            finalPhotoUrl = finalPhotoUrl[startIndex..];
                        }
                    }
                }

                var file = form.Files.GetFile("image");

                if (file is { Length: > 0 })
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                        return Results.BadRequest("Unsupported image format.");

                    var uploadFolder = Path.Combine(env.WebRootPath, "images", "ingredients");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    try
                    {
                        using (var image = await Image.LoadAsync(file.OpenReadStream()))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Mode = ResizeMode.Max,
                                Size = new Size(1200, 0)
                            }));

                            await image.SaveAsJpegAsync(filePath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                            {
                                Quality = 80
                            });
                        }

                        if (!string.IsNullOrEmpty(ingredient.ImageUrl) && ingredient.ImageUrl != "default_ingredient_picture.png")
                        {
                            var oldFilePath = Path.Combine(uploadFolder, ingredient.ImageUrl);
                            if (File.Exists(oldFilePath))
                            {
                                File.Delete(oldFilePath);
                            }
                        }

                        finalPhotoUrl = fileName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Image Processing Error: {ex.Message}");
                    }
                }

                try
                {
                    var ct = await db.ShoppingCategories.AsNoTracking().FirstOrDefaultAsync(ct => ct.Id == dto.ShoppingCategoryId);

                    if (ct == null)
                    {
                        ct = await db.ShoppingCategories
                             .AsNoTracking()
                             .FirstOrDefaultAsync(t => t.Code == "Uncategorized");
                    }

                    ingredient.Names = dto.Names;
                    ingredient.Barcode = dto.Barcode;
                    ingredient.ImageUrl = finalPhotoUrl;
                    ingredient.ShoppingCategoryId = ct.Id;
                    ingredient.DefaultUnitId = dto.DefaultUnitId;
                    ingredient.OffCategoryTag = dto.OffCategoryTag;
                    ingredient.Barcode = dto.Barcode;

                    ingredient.Calories = dto.Calories;
                    ingredient.Proteins = dto.Proteins;
                    ingredient.Fats = dto.Fats;
                    ingredient.SaturatedFats = dto.SaturatedFats;
                    ingredient.Carbohydrates = dto.Carbohydrates;
                    ingredient.Sugars = dto.Sugars;
                    ingredient.Fiber = dto.Fiber;
                    ingredient.Salt = dto.Salt;

                    ingredient.NutriScore = dto.NutriScore;
                    ingredient.NovaClassification = dto.NovaClassification;
                    ingredient.IsVegan = dto.IsVegan;
                    ingredient.IsVegetarian = dto.IsVegetarian;
                    ingredient.IsGlutenFree = dto.IsGlutenFree;
                    ingredient.IsLactoseFree = dto.IsLactoseFree;


                    db.IngredientUnits.RemoveRange(ingredient.IngredientUnits);

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

                    await db.SaveChangesAsync();

                    return Results.Ok("Ingredient updated successfully");
                }
                catch (Exception ex)
                {
                    return Results.Problem($"An error occurred while updating the ingredient. Ex: {ex.Message}");
                }

            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Toggle ingredient approval
            group.MapPost("/admin/toggleApproval/{ingredientId:guid}", async (Guid ingredientId, PinulaDbContext db) =>
            {
                var ingredient = await db.Ingredients.FirstOrDefaultAsync(r => r.Id == ingredientId);
                if (ingredient is null) return Results.NotFound("Ingredient not found");

                ingredient.IsApproved = !ingredient.IsApproved;

                await db.SaveChangesAsync();
                return Results.Ok(new { isApproved = ingredient.IsApproved });

            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Toggle ingredient checked
            group.MapPost("/admin/toggleChecked/{ingredientId:guid}", async (Guid ingredientId, PinulaDbContext db) =>
            {
                var ingredient = await db.Ingredients.FirstOrDefaultAsync(r => r.Id == ingredientId);
                if (ingredient is null) return Results.NotFound("Ingredient not found");

                ingredient.Checked = !ingredient.Checked;

                await db.SaveChangesAsync();
                return Results.Ok(new { Checked = ingredient.Checked });

            }).RequireAuthorization("AdminOnly");



        }

    }
}
