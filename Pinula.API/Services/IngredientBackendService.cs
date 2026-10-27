using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.API.Endpoints;
using Pinula.API.Interface;
using Pinula.API.Models;
using Pinula.API.Projections;
using Pinula.Shared.Constants;
using Pinula.Shared.DTOs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Pinula.API.Services;

public class IngredientBackendService : IIngredientBackendService
{
    private readonly PinulaDbContext _db;
    private readonly ITranslationService _translationService;
    private readonly ILogger<IngredientBackendService> _logger;

    public IngredientBackendService(PinulaDbContext db, ITranslationService translationService, ILogger<IngredientBackendService> logger)
    {
        _db = db;
        _translationService = translationService;
        _logger = logger;
    }
    
    public async Task<List<IngredientPreviewDto>> GetFilteredPreviews(IngredientFilterParameters filter, string baseUrl, string lang)
    {
        var query = _db.Ingredients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Barcode))
        {
            query = query.Where(i => i.Barcode == filter.Barcode);
        }
        else if (string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            return new List<IngredientPreviewDto>();
        }
        
        var rawIngredients = await query
            .AsNoTracking()
            .Include(i => i.DefaultUnit)
            .Include(i => i.IngredientUnits)
                .ThenInclude(iu => iu.Unit)
            .ToListAsync();
        
        if (string.IsNullOrWhiteSpace(filter.Barcode) && !string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTokens = filter.SearchTerm
                .RemoveDiacritics()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            rawIngredients = rawIngredients
                .Select(i => new
                {
                    Ingredient = i,
                    NormalizedNames = new[]
                    {
                        i.Names.GetValueOrDefault(lang),
                        i.Names.GetValueOrDefault("en")
                    }
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Select(name => name!.RemoveDiacritics().ToLowerInvariant())
                    .ToList()
                })
                .Where(x => searchTokens.All(token => x.NormalizedNames.Any(name => name.Contains(token))))
                .OrderByDescending(x => x.NormalizedNames.Any(name => name.StartsWith(searchTokens[0])))
                .Select(x => x.Ingredient)
                .ToList();
        }
        
        return rawIngredients
            .Take(filter.Amount)
            .Select(IngredientProjections.ToPreviewDto(baseUrl, lang).Compile())
            .ToList();
    }

    public async Task<CreateIngredientResponse> Create(IngredientCreateDto createDto, IFormFile? imageFile, IWebHostEnvironment webHostEnvironment, string lang, Guid userId)
    {
        var mainName = createDto.Names.GetValueOrDefault(lang);
        if (string.IsNullOrWhiteSpace(mainName)) 
            return new CreateIngredientResponse { StatusCode = 400, Message = "Missing ingredient data" };
        
        var csName = createDto.Names.GetValueOrDefault("cs");
        var enName = createDto.Names.GetValueOrDefault("en");

        if (string.IsNullOrWhiteSpace(csName) && !string.IsNullOrWhiteSpace(enName))
        {
            var translated = await _translationService.TranslateTextAsync(enName, "cs");
            createDto.Names["cs"] = translated ?? enName;
        }
        else if (string.IsNullOrWhiteSpace(enName) && !string.IsNullOrWhiteSpace(csName))
        {
            var translated = await _translationService.TranslateTextAsync(csName, "en");
            createDto.Names["en"] = translated ?? csName;
        }

        if (string.IsNullOrWhiteSpace(createDto.Names.GetValueOrDefault("cs")) && 
            string.IsNullOrWhiteSpace(createDto.Names.GetValueOrDefault("en")))
        {
            return new CreateIngredientResponse { StatusCode = 500, Message = "Unable to process ingredient names data" };
        }
        
        var newId = Guid.NewGuid();
        var finalPhotoUrl = ImageConstants.DefaultIngredient;

        if (imageFile is { Length: > 0 })
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return new CreateIngredientResponse { StatusCode = 400, Message = "Unsupported image file format" };

            var uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "ingredients");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            var fileName = $"{newId}_{Guid.NewGuid()}.webp";
            var filePath = Path.Combine(uploadFolder, fileName);

            try
            {
                using (var image = await Image.LoadAsync(imageFile.OpenReadStream()))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(1200, 0)
                    }));

                    await image.SaveAsWebpAsync(filePath, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder { Quality = 80 });
                }
                finalPhotoUrl = fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing image for ingredient creation");
                return new CreateIngredientResponse { StatusCode = 500, Message = "Unable to process image" };
            }
        }

        try
        {
            Guid scId;
            if (createDto.ShoppingCategoryId is null)
            {
                var category = await _db.ShoppingCategories.AsNoTracking().FirstOrDefaultAsync(t => t.Code == "UNCATEGORIZED")
                            ?? await _db.ShoppingCategories.AsNoTracking().FirstOrDefaultAsync();

                if (category is null)
                    return new CreateIngredientResponse { StatusCode = 500, Message = "No shopping categories available in database." };

                scId = category.Id;
            }
            else
            {
                scId = createDto.ShoppingCategoryId.Value;
            }
            
            var ingredient = new Ingredient
            {
                Id = newId,
                UserId = userId,
                Names = createDto.Names,
                Barcode = createDto.Barcode,
                ImageUrl = finalPhotoUrl,
                ShoppingCategoryId = scId,
                DefaultUnitId = createDto.DefaultUnitId,
                IngredientCreated = DateTime.UtcNow,
                BaseIngredientId = createDto.BaseIngredientId,
                OffCategoryTag = createDto.OffCategoryTag,
                EdibleRatio = createDto.EdibleRatio,

                Calories = createDto.Calories,
                Proteins = createDto.Proteins,
                Fats = createDto.Fats,
                SaturatedFats = createDto.SaturatedFats,
                Carbohydrates = createDto.Carbohydrates,
                Sugars = createDto.Sugars,
                Fiber = createDto.Fiber,
                Salt = createDto.Salt,

                NutriScore = createDto.NutriScore,
                NovaClassification = createDto.NovaClassification,
                IsVegan = createDto.IsVegan,
                IsVegetarian = createDto.IsVegetarian,
                IsGlutenFree = createDto.IsGlutenFree,
                IsLactoseFree = createDto.IsLactoseFree
            };
            
            foreach (var iu in createDto.AdditionalUnits)
            {
                ingredient.IngredientUnits.Add(new IngredientUnit
                {
                    IngredientId = ingredient.Id,
                    UnitId = iu.UnitId,
                    AmountInGrams = iu.ToDefaultUnit
                });
            }
            
            var systemUnits = await _db.Units
                .AsNoTracking()
                .Where(u => u.Code == "g" || u.Code == "kg" || u.Code == "ml" || u.Code == "l")
                .ToDictionaryAsync(u => u.Code, u => u.Id);

            if (systemUnits.TryGetValue("g", out var gId) && !ingredient.IngredientUnits.Any(u => u.UnitId == gId))
            {
                ingredient.IngredientUnits.Add(new IngredientUnit { IngredientId = ingredient.Id, UnitId = gId, AmountInGrams = 1 });
            }

            if (systemUnits.TryGetValue("kg", out var kgId) && !ingredient.IngredientUnits.Any(u => u.UnitId == kgId))
            {
                ingredient.IngredientUnits.Add(new IngredientUnit { IngredientId = ingredient.Id, UnitId = kgId, AmountInGrams = 1000 });
            }
            
            if (systemUnits.TryGetValue("ml", out var mlId) && systemUnits.TryGetValue("l", out var lId))
            {
                var existingMl = ingredient.IngredientUnits.FirstOrDefault(u => u.UnitId == mlId);
                var existingL = ingredient.IngredientUnits.FirstOrDefault(u => u.UnitId == lId);

                if (existingMl != null && existingL == null)
                {
                    ingredient.IngredientUnits.Add(new IngredientUnit
                    {
                        IngredientId = ingredient.Id,
                        UnitId = lId,
                        AmountInGrams = existingMl.AmountInGrams * 1000
                    });
                }
                else if (existingL != null && existingMl == null)
                {
                    ingredient.IngredientUnits.Add(new IngredientUnit
                    {
                        IngredientId = ingredient.Id,
                        UnitId = mlId,
                        AmountInGrams = existingL.AmountInGrams / 1000
                    });
                }
            }

            _db.Ingredients.Add(ingredient);
            await _db.SaveChangesAsync();

            return new CreateIngredientResponse
            {
                StatusCode = 201,
                Message = "Ingredient successfully created",
                CreatedIngredientId = ingredient.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while saving ingredient with ID {IngredientId}", newId);
            return new CreateIngredientResponse { StatusCode = 500, Message = "An error occurred while saving the ingredient." };
        }
    }

    public async Task<List<AdminIngredientPreviewDto>> AdminGetPreviews(int amount, int skip, string baseUrl)
    {
        var list = await _db.Ingredients.AsNoTracking().Skip(skip).Take(amount).Select(i => new AdminIngredientPreviewDto
        {
            Id = i.Id,
            IsDeleted = i.IsDeleted,
            IsApproved = i.IsApproved,
            IngredientCreated = i.IngredientCreated,
            Checked = i.Checked,
            Names = i.Names,
            ImageUrl = $"{baseUrl}{(string.IsNullOrWhiteSpace(i.ImageUrl) ? ImageConstants.DefaultIngredient : i.ImageUrl)}",
        }).ToListAsync();

        return list;
    }

    public async Task<AdminIngredientDisplayDto?> AdminGetDetails(Guid id, string baseUrl, string lang)
    {
        var ingredientDb = await _db.Ingredients
            .AsNoTrackingWithIdentityResolution()
            .Include(i => i.DefaultUnit)
            .Include(i => i.ShoppingCategory)
            .Include(i => i.BaseIngredient)
            .Include(i => i.Creator)
            .Include(i => i.IngredientUnits)
            .ThenInclude(iu => iu.Unit)
            .FirstOrDefaultAsync(i => i.Id == id);
        
        if (ingredientDb is null) return null;
        return ingredientDb.AdaptWithLangAndBaseUrl<AdminIngredientDisplayDto>(baseUrl, lang);
    }
    
    public async Task<GeneralResponse> AdminDelete(Guid id)
    {
        var ingredient = await _db.Ingredients.FirstOrDefaultAsync(i => i.Id == id);
        if (ingredient is null) return new GeneralResponse() { Successful = false, StatusCode = 404, Message = $"Ingredient with id:{id} does not exist." };

        if(await _db.RecipeIngredients.AnyAsync(ri => ri.IngredientId == id)){
            ingredient.IsDeleted = true;
            await _db.SaveChangesAsync();
            return new GeneralResponse() { Successful = true, StatusCode = 200, Message = $"Ingredient with id:{id} is used in recipes, successfully SOFT deleted." };
        }
        else
        {
            _db.Remove(ingredient);
            await _db.SaveChangesAsync();
            return new GeneralResponse() { Successful = true, StatusCode = 200, Message = $"Ingredient with id:{id} successfully permanently deleted." };
        }
    }

    public async Task<GeneralResponse> AdminUpdate(IngredientCreateDto dto, IFormFile? imageFile, IWebHostEnvironment webHostEnvironment, string lang, Guid userId)
    {
        var ingredient = await _db.Ingredients
            .Include(i => i.IngredientUnits)
            .FirstOrDefaultAsync(i => i.Id == dto.Id);

        if (ingredient == null)
        {
            return new GeneralResponse 
            { 
                Successful = false, 
                StatusCode = 404, 
                Message = $"Ingredient with id:{dto.Id} does not exist." 
            };
        }

        var uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "ingredients");
        var finalPhotoUrl = ingredient.ImageUrl;
        
        if (imageFile is { Length: > 0 })
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return new GeneralResponse 
                { 
                    Successful = false, 
                    StatusCode = 400, 
                    Message = "Unsupported image format." 
                };
            }

            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            var newFileName = $"{ingredient.Id}_{Guid.NewGuid()}.webp";
            var newFilePath = Path.Combine(uploadFolder, newFileName);

            try
            {
                using (var image = await Image.LoadAsync(imageFile.OpenReadStream()))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(1200, 0)
                    }));

                    await image.SaveAsWebpAsync(newFilePath, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder
                    {
                        Quality = 80
                    });
                }
                
                if (!string.IsNullOrEmpty(ingredient.ImageUrl) && 
                    ingredient.ImageUrl != ImageConstants.DefaultIngredient && 
                    ingredient.ImageUrl != "default_ingredient.png")
                {
                    var oldFileName = Path.GetFileName(ingredient.ImageUrl);
                    var oldFilePath = Path.Combine(uploadFolder, oldFileName);
                    
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }

                finalPhotoUrl = newFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing image update for ingredient ID {IngredientId}", ingredient.Id);
                return new GeneralResponse 
                { 
                    Successful = false, 
                    StatusCode = 500, 
                    Message = "Unable to process uploaded image." 
                };
            }
        }

        try
        {
            Guid shoppingCategoryId;
            if (dto.ShoppingCategoryId.HasValue)
            {
                var categoryExists = await _db.ShoppingCategories.AnyAsync(sc => sc.Id == dto.ShoppingCategoryId.Value);
                if (categoryExists)
                {
                    shoppingCategoryId = dto.ShoppingCategoryId.Value;
                }
                else
                {
                    var category = await _db.ShoppingCategories.AsNoTracking().FirstOrDefaultAsync(t => t.Code == "UNCATEGORIZED")
                                   ?? await _db.ShoppingCategories.AsNoTracking().FirstOrDefaultAsync();

                    shoppingCategoryId = category.Id;
                }
            }
            else
            {
                var category = await _db.ShoppingCategories.AsNoTracking().FirstOrDefaultAsync(t => t.Code == "UNCATEGORIZED")
                               ?? await _db.ShoppingCategories.AsNoTracking().FirstOrDefaultAsync();

                shoppingCategoryId = category.Id;
            }
            
            ingredient.Names = dto.Names;
            ingredient.Barcode = dto.Barcode;
            ingredient.ImageUrl = finalPhotoUrl;
            ingredient.ShoppingCategoryId = shoppingCategoryId;
            ingredient.DefaultUnitId = dto.DefaultUnitId;
            ingredient.OffCategoryTag = dto.OffCategoryTag;
            ingredient.BaseIngredientId = dto.BaseIngredientId;

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
            
            _db.IngredientUnits.RemoveRange(ingredient.IngredientUnits);

            if (dto.AdditionalUnits != null && dto.AdditionalUnits.Count > 0)
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

            await _db.SaveChangesAsync();

            return new GeneralResponse 
            { 
                Successful = true, 
                StatusCode = 200, 
                Message = "Ingredient updated successfully" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating ingredient ID {IngredientId}", dto.Id);
            return new GeneralResponse 
            { 
                Successful = false, 
                StatusCode = 500, 
                Message = "An error occurred while updating the ingredient." 
            };
        }
    }

    public async Task<GeneralResponse> AdminToggleApproval(Guid id)
    {
        var ingredient = await _db.Ingredients.FirstOrDefaultAsync(i => i.Id == id);
        if (ingredient is null)
        {
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Ingredient not found." };
        }

        ingredient.IsApproved = !ingredient.IsApproved;
        await _db.SaveChangesAsync();

        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Ingredient approval status toggled." };
    }

    public async Task<GeneralResponse> AdminToggleChecked(Guid id)
    {
        var ingredient = await _db.Ingredients.FirstOrDefaultAsync(i => i.Id == id);
        if (ingredient is null)
        {
            return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Ingredient not found." };
        }

        ingredient.Checked = !ingredient.Checked;
        await _db.SaveChangesAsync();

        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Ingredient checked status toggled." };
    }

    public async Task<List<ShoppingCategoryDisplayDto>> GetShoppingCategories(string baseUrl, string lang)
    {
        var categoriesRaw = await _db.ShoppingCategories
            .AsNoTracking()
            .ToListAsync();

        return categoriesRaw.AdaptWithLangAndBaseUrl<List<ShoppingCategoryDisplayDto>>(baseUrl, lang);
    }
    
    
}