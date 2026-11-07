using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pinula.API;
using Pinula.API.Context;
using Pinula.API.Interface;
using Pinula.API.Models;
using Pinula.Shared.Constants;
using Pinula.Shared.DTOs;
using Pinula.Shared.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public class RecipeBackendService : IRecipeBackendService
{
    private readonly PinulaDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ITranslationService _translationService;
    private readonly ILogger<RecipeBackendService> _logger;

    public RecipeBackendService(PinulaDbContext db, IWebHostEnvironment env, ITranslationService translationService, ILogger<RecipeBackendService> logger)
    {
        _db = db;
        _env = env;
        _translationService = translationService;
        _logger = logger;
    }

    public async Task<List<RecipePreviewDto>> GetFilteredPreviewsAsync(RecipeFilterParameters filter, Guid? currentUserId, string baseUrl, string lang)
    {
        var imageBaseUrl = $"{baseUrl}/images/recipes/";
        var defaultImage = ImageConstants.DefaultRecipe;

        var userDb = currentUserId.HasValue 
            ? await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId) 
            : null;

        var query = _db.Recipes.AsNoTracking().AsQueryable();

        if (userDb is not null && userDb.Role == "admin" && filter.IncludeUnapproved)
        {
            //Admin
        }
        else
        {
            query = query.Where(r => r.IsApproved && !r.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(r => r.Titles.GetValueOrDefault(lang).ToLower().Contains(filter.SearchTerm.ToLower()));

        if (filter.CategoryId.HasValue)
            query = query.Where(r => r.Categories.Any(c => c.Id == filter.CategoryId));

        if (filter.MaxCookingTime.HasValue)
            query = query.Where(r => r.CookingTime <= filter.MaxCookingTime);

        if (filter.MinRating.HasValue)
            query = query.Where(r => r.Rating >= filter.MinRating);

        if (filter.MaxDifficulty.HasValue)
            query = query.Where(r => r.Difficulty <= filter.MaxDifficulty);

        if (filter.MaxCalories.HasValue)
            query = query.Where(r => r.Calories <= filter.MaxCalories);

        if (filter.OnlyFavorites && currentUserId.HasValue)
            query = query.Where(r => r.RecipeUsers.Any(ru => ru.UserId == currentUserId && ru.IsFavorite));

        if (filter.OnlyMine && currentUserId.HasValue)
            query = query.Where(r => r.UserId == currentUserId);

        query = filter.Sort switch
        {
            SortBy.Rating => filter.SortDescending ? query.OrderByDescending(r => r.Rating) : query.OrderBy(r => r.Rating),
            SortBy.CookingTime => filter.SortDescending ? query.OrderByDescending(r => r.CookingTime) : query.OrderBy(r => r.CookingTime),
            SortBy.Calories => filter.SortDescending ? query.OrderByDescending(r => r.Calories) : query.OrderBy(r => r.Calories),
            SortBy.Oldest => query.OrderBy(r => r.RecipeCreated),
            _ => query.OrderByDescending(r => r.RecipeCreated)
        };
        
        return await query
            .Skip(filter.Skip)
            .Take(filter.Amount)
            .Select(r => new RecipePreviewDto
            {
                Id = r.Id,
                Title = r.Titles.GetValueOrDefault(lang) ?? r.Titles.GetValueOrDefault("en") ?? "Title",
                PhotoUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(r.PhotoUrl) ? defaultImage : r.PhotoUrl)}",
                CookingTime = r.CookingTime,
                Difficulty = (DifficultyLevel)r.Difficulty,
                Rating = r.Rating,
                UserName = r.User.Name,
                Calories = r.Calories,
                ServingsAmount = r.ServingsAmount,
                IsFavorite = currentUserId != null && r.RecipeUsers.Any(ru => ru.UserId == currentUserId && ru.IsFavorite),
                IsApproved = r.IsApproved,
                IsDeleted = r.IsDeleted,
                MacrosLabel = GetMacrosLabel(r.Calories, r.Proteins, r.Fats, r.Carbohydrates),
                Checked = r.Checked,
            }).ToListAsync();
    }

    public async Task<RecipeDetailsDto?> GetRecipeDetailsAsync(Guid recipeId, Guid? currentUserId, string baseUrl, string lang)
    {
        var recipeDb = await _db.Recipes.AsNoTrackingWithIdentityResolution()
            .Include(r => r.User)
            .Include(r => r.ServingUnit)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Ingredient)
            .Include(r => r.RecipeIngredients).ThenInclude(ri => ri.Unit)
            .Include(r => r.RecipeSteps)
            .Include(r => r.Categories)
            .FirstOrDefaultAsync(r => r.Id == recipeId);

        if (recipeDb == null) return null;

        var recipe = recipeDb.AdaptWithLangAndBaseUrl<RecipeDetailsDto>(baseUrl, lang);
        
        var allComments = await _db.Comments.AsNoTracking()
            .Where(c => c.RecipeId == recipeId)
            .Select(c => new CommentDisplayDto
            {
                Id = c.Id,
                Text = c.Text ?? string.Empty,
                Rating = c.Rating,
                CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                UserId = c.User.Id,
                UserName = c.User.Name,
                UserSurname = c.User.Surname,
                IsApproved = c.IsApproved,
                IsEdited = c.IsEdited,
                IsDeleted = c.IsDeleted,
                EditedAt = c.EditedAt,
                ParentCommentId = c.ParentCommentId,
                Replies = new List<CommentDisplayDto>()
            }).OrderByDescending(c => c.CreatedAt).ToListAsync();

        var commentLookup = allComments.ToDictionary(c => c.Id);
        var rootComments = new List<CommentDisplayDto>();

        foreach (var comment in allComments)
        {
            if (comment.ParentCommentId.HasValue && commentLookup.ContainsKey(comment.ParentCommentId.Value))
            {
                commentLookup[comment.ParentCommentId.Value].Replies.Add(comment);
            }
            else
            {
                rootComments.Add(comment);
            }
        }
        recipe.Comments = rootComments;

        if (currentUserId == null || currentUserId == Guid.Empty) return recipe;

        var userDb = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId);
        if (userDb is null) return recipe;

        recipe.UserAlreadyRated = allComments.Any(c => c.UserId == currentUserId && c.Rating != null && c.IsApproved);
        recipe.IsFavorite = await _db.RecipeUsers.AnyAsync(ru => ru.UserId == currentUserId && ru.IsFavorite);

        if (userDb.GroupId is null) return recipe;
        
        var ingredientIds = recipe.RecipeIngredients.Select(ri => ri.IngredientId).Distinct().ToList();
        var inventoryItemsDb = await _db.InventoryItems
            .AsNoTracking()
            .Include(i => i.Ingredient)
            .Include(i => i.Allocations)
            .Where(i => i.GroupId == userDb.GroupId && (ingredientIds.Contains(i.IngredientId) || (i.Ingredient.BaseIngredientId.HasValue && ingredientIds.Contains(i.Ingredient.BaseIngredientId.Value))))
            .ToListAsync();

        foreach (var ingredient in recipe.RecipeIngredients)
        {
            var inventoryIngredients = inventoryItemsDb.Where(i => i.IngredientId == ingredient.IngredientId || i.Ingredient.BaseIngredientId == ingredient.IngredientId);
            
            decimal totalFreeGrams = 0;
            foreach (var invItem in inventoryIngredients)
            {
                var allocatedGrams = invItem.Allocations.Sum(a => a.AllocatedQuantityInGrams);
                var freeGrams = invItem.QuantityInGrams - allocatedGrams;
                if (freeGrams > 0) totalFreeGrams += freeGrams;
            }
            
            ingredient.QuantityInInventory = totalFreeGrams > 0 && ingredient.ConversionFactor > 0 ? totalFreeGrams / ingredient.ConversionFactor : 0;
        }

        return recipe;
    }

    public async Task<GeneralResponse> CreateRecipeAsync(Guid userId, RecipeCreateDto dto, IFormFile? imageFile, string lang)
    {
        var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (dbUser is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "User not found." };
        if (!dbUser.CanCreateRecipes) return new GeneralResponse { Successful = false, StatusCode = 403, Message = "Forbidden" };

        string finalPhotoUrl = ImageConstants.DefaultRecipe;
        Guid newRecipeGuid = Guid.NewGuid();
        
        if (imageFile is { Length: > 0 })
        {
            var processedImage = await ProcessAndSaveImageAsync(imageFile, newRecipeGuid);
            if (processedImage.IsSuccess)
            {
                finalPhotoUrl = processedImage.FileName;
            }
            else
            {
                return new GeneralResponse { Successful = false, StatusCode = 400, Message = processedImage.Error };
            }
        }

        var ingredientIds = dto.RecipeIngredients.Select(x => x.Ingredient.Id).ToList();
        var dbIngredients = await _db.Ingredients.Include(x => x.IngredientUnits).Where(x => ingredientIds.Contains(x.Id)).ToListAsync();
        var dbCategories = await _db.Categories.Where(x => dto.CategoriesIds.Contains(x.Id)).ToListAsync();

        string targetLang = lang == "en" ? "cs" : "en";
        var titleTranslated = await _translationService.TranslateTextAsync(dto.Title, targetLang) ?? dto.Title;
        var titles = new Dictionary<string, string> { { lang, dto.Title }, { targetLang, titleTranslated } };

        var newRecipe = new Recipe
        {
            Id = newRecipeGuid,
            UserId = userId,
            OriginalLanguage = lang,
            Titles = titles,
            PhotoUrl = finalPhotoUrl,
            CookingTime = dto.CookingTime,
            ServingsAmount = dto.ServingsAmount,
            ServingUnitId = dto.ServingUnit,
            Difficulty = dto.Difficulty,
            RecipeCreated = DateTime.UtcNow,
            Rating = 0,
            UsersRated = 0,
            
            Calories = 0,
            Proteins = 0,
            Fats = 0,
            SaturatedFats = 0,
            Carbohydrates = 0,
            Sugars = 0,
            Fiber = 0,
            Salt = 0,

            IsGlutenFree = true,
            IsLactoseFree = true,
            IsVegan = true,
            IsVegetarian = true
        };

        foreach (var i in dto.RecipeIngredients)
        {
            var dbIng = dbIngredients.FirstOrDefault(x => x.Id == i.Ingredient.Id);
            if (dbIng != null)
            {
                var ingredientUnit = dbIng.IngredientUnits.FirstOrDefault(iu => iu.UnitId == i.Unit.Id);
                decimal conversionFactor = ingredientUnit?.AmountInGrams ?? 1;
                decimal factor = (conversionFactor / 100) * (i.Quantity / dto.ServingsAmount);

                newRecipe.Calories += factor * dbIng.Calories;
                newRecipe.Proteins += factor * dbIng.Proteins;
                newRecipe.Fats += factor * dbIng.Fats;
                newRecipe.SaturatedFats += factor * dbIng.SaturatedFats;
                newRecipe.Carbohydrates += factor * dbIng.Carbohydrates;
                newRecipe.Sugars += factor * dbIng.Sugars;
                newRecipe.Fiber += factor * dbIng.Fiber;
                newRecipe.Salt += factor * dbIng.Salt;
                
                newRecipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    RecipeId = newRecipe.Id,
                    IngredientId = i.Ingredient.Id,
                    Quantity = i.Quantity,
                    UnitId = i.Unit.Id,
                    ConversionFactor = conversionFactor
                });

                if (!dbIng.IsGlutenFree) newRecipe.IsGlutenFree = false;
                if (!dbIng.IsLactoseFree) newRecipe.IsLactoseFree = false;
                if (!dbIng.IsVegan) newRecipe.IsVegan = false;
                if (!dbIng.IsVegetarian) newRecipe.IsVegetarian = false;
            }
        }

        foreach (var step in dto.RecipeSteps)
        {
            var descriptionTranslated = await _translationService.TranslateTextAsync(step.Description, targetLang) ?? step.Description;
            newRecipe.RecipeSteps.Add(new RecipeStep
            {
                Id = Guid.NewGuid(),
                RecipeId = newRecipe.Id,
                Descriptions = new Dictionary<string, string> { { lang, step.Description }, { targetLang, descriptionTranslated } },
                StepNumber = step.StepNumber
            });
        }

        foreach (var category in dbCategories)
        {
            newRecipe.Categories.Add(category);
        }

        _db.Recipes.Add(newRecipe);
        await _db.SaveChangesAsync();

        return new GeneralResponse { Successful = true, StatusCode = 201, Id = newRecipe.Id, Message = "Recipe created" };
    }

    public async Task<GeneralResponse> UpdateRecipeAsync(Guid id, Guid userId, RecipeCreateDto dto, IFormFile? imageFile, string lang)
    {
        var existingRecipe = await _db.Recipes.Include(r => r.Categories).FirstOrDefaultAsync(r => r.Id == id);
        if (existingRecipe is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Recipe not found." };
        if (existingRecipe.UserId != userId) return new GeneralResponse { Successful = false, StatusCode = 403, Message = "Forbidden" };

        if (imageFile is { Length: > 0 })
        {
            var processedImage = await ProcessAndSaveImageAsync(imageFile, existingRecipe.Id);
            if (processedImage.IsSuccess)
            {
                if (!string.IsNullOrEmpty(existingRecipe.PhotoUrl) && existingRecipe.PhotoUrl != ImageConstants.DefaultRecipe)
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, "images", "recipes", existingRecipe.PhotoUrl);
                    if (File.Exists(oldFilePath)) File.Delete(oldFilePath);
                }
                existingRecipe.PhotoUrl = processedImage.FileName;
            }
            else
            {
                return new GeneralResponse { Successful = false, StatusCode = 400, Message = processedImage.Error };
            }
        }

        try
        {
            await _db.RecipeIngredients.Where(ri => ri.RecipeId == id).ExecuteDeleteAsync();
            
            var oldSteps = await _db.RecipeSteps.Where(rs => rs.RecipeId == id).ToListAsync();
            await _db.RecipeSteps.Where(rs => rs.RecipeId == id).ExecuteDeleteAsync();

            string targetLang = lang == "en" ? "cs" : "en";

            if (existingRecipe.Titles.GetValueOrDefault(lang) != dto.Title)
            {
                var titleTranslated = await _translationService.TranslateTextAsync(dto.Title, targetLang) ?? dto.Title;
                existingRecipe.Titles[lang] = dto.Title;
                existingRecipe.Titles[targetLang] = titleTranslated;
            }

            existingRecipe.CookingTime = dto.CookingTime;
            existingRecipe.ServingsAmount = dto.ServingsAmount;
            existingRecipe.ServingUnitId = dto.ServingUnit;
            existingRecipe.Difficulty = dto.Difficulty;

            existingRecipe.Calories = existingRecipe.Proteins = existingRecipe.Fats = existingRecipe.Carbohydrates = existingRecipe.Fiber = 0;

            var categoriesToRemove = existingRecipe.Categories.Where(c => !dto.CategoriesIds.Contains(c.Id)).ToList();
            foreach (var c in categoriesToRemove) existingRecipe.Categories.Remove(c);

            var existingCatIds = existingRecipe.Categories.Select(c => c.Id).ToList();
            var categoriesToAddIds = dto.CategoriesIds.Except(existingCatIds).ToList();

            if (categoriesToAddIds.Any())
            {
                var catsToAdd = await _db.Categories.Where(c => categoriesToAddIds.Contains(c.Id)).ToListAsync();
                foreach (var c in catsToAdd) existingRecipe.Categories.Add(c);
            }

            var ingredientIds = dto.RecipeIngredients.Select(x => x.Ingredient.Id).ToList();
            var dbIngredients = await _db.Ingredients.Include(x => x.IngredientUnits).Where(x => ingredientIds.Contains(x.Id)).ToListAsync();
            var newIngredients = new List<RecipeIngredient>();

            foreach (var i in dto.RecipeIngredients)
            {
                var dbIng = dbIngredients.FirstOrDefault(x => x.Id == i.Ingredient.Id);
                if (dbIng != null)
                {
                    var ingredientUnit = dbIng.IngredientUnits.FirstOrDefault(iu => iu.UnitId == i.Unit.Id);
                    decimal conversionFactor = ingredientUnit?.AmountInGrams ?? 1;
                    decimal factor = (conversionFactor / 100) * (i.Quantity / dto.ServingsAmount);

                    existingRecipe.Calories += factor * dbIng.Calories;
                    existingRecipe.Proteins += factor * dbIng.Proteins;
                    existingRecipe.Fats += factor * dbIng.Fats;
                    existingRecipe.Carbohydrates += factor * dbIng.Carbohydrates;
                    existingRecipe.Fiber += factor * dbIng.Fiber;
                }

                newIngredients.Add(new RecipeIngredient
                {
                    RecipeId = existingRecipe.Id,
                    IngredientId = i.Ingredient.Id,
                    Quantity = i.Quantity,
                    UnitId = i.Unit.Id,
                    ConversionFactor = i.ConversionFactor
                });
            }
            _db.RecipeIngredients.AddRange(newIngredients);

            var newSteps = new List<RecipeStep>();
            foreach (var step in dto.RecipeSteps)
            {
                var newStep = new RecipeStep { Id = Guid.NewGuid(), RecipeId = existingRecipe.Id, StepNumber = step.StepNumber, Descriptions = new Dictionary<string, string>() };
                var oldStep = oldSteps.FirstOrDefault(s => s.StepNumber == step.StepNumber);

                if (oldStep != null && step.Description == oldStep.Descriptions.GetValueOrDefault(lang))
                {
                    newStep.Descriptions = oldStep.Descriptions;
                }
                else
                {
                    var descriptionTranslated = await _translationService.TranslateTextAsync(step.Description, targetLang) ?? step.Description;
                    newStep.Descriptions[lang] = step.Description;
                    newStep.Descriptions[targetLang] = descriptionTranslated;
                }
                newSteps.Add(newStep);
            }
            _db.RecipeSteps.AddRange(newSteps);

            await _db.SaveChangesAsync();
            return new GeneralResponse { Successful = true, StatusCode = 200, Id = existingRecipe.Id, Message = "Recipe updated successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating recipe {RecipeId}", id);
            return new GeneralResponse { Successful = false, StatusCode = 500, Message = "An error occurred while updating the recipe." };
        }
    }

    public async Task<GeneralResponse> DeleteRecipeAsync(Guid id, Guid userId)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == id);
        if (recipe is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Recipe not found" };
        if (recipe.UserId != userId) return new GeneralResponse { Successful = false, StatusCode = 403, Message = "Forbidden" };

        recipe.IsDeleted = true;
        await _db.SaveChangesAsync();
        
        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Recipe deleted" };
    }

    public async Task<ToggleFavoriteResponse> ToggleFavoriteAsync(Guid recipeId, Guid userId)
    {
        var recipeExists = await _db.Recipes.AnyAsync(r => r.Id == recipeId);
        if (!recipeExists) return new ToggleFavoriteResponse { Successful = false, StatusCode = 404, Message = "Recipe not found" };

        var favorite = await _db.RecipeUsers.FirstOrDefaultAsync(ru => ru.RecipeId == recipeId && ru.UserId == userId);

        if (favorite == null)
        {
            _db.RecipeUsers.Add(new RecipeUser { RecipeId = recipeId, UserId = userId, IsFavorite = true });
            await _db.SaveChangesAsync();
            return new ToggleFavoriteResponse { Successful = true, StatusCode = 200, IsFavorite = true };
        }
        
        favorite.IsFavorite = !favorite.IsFavorite;
        await _db.SaveChangesAsync();
        return new ToggleFavoriteResponse { Successful = true, StatusCode = 200, IsFavorite = favorite.IsFavorite };
    }

    public async Task<CommentPostResponse> PostCommentAsync(Guid userId, CommentCreateDto dto, string lang)
    {
        var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (dbUser is null) return new CommentPostResponse { Successful = false, StatusCode = 404, Message = "User not found." };
        if (!dbUser.CanComment) return new CommentPostResponse { Successful = false, StatusCode = 403, Message = "Forbidden" };

        var exists = await _db.Recipes.AnyAsync(r => r.Id == dto.RecipeId);
        if (!exists) return new CommentPostResponse { Successful = false, StatusCode = 404, Message = "Recipe not found" };

        if (dto.ParentCommentId.HasValue)
        {
            if (!await _db.Comments.AnyAsync(c => c.Id == dto.ParentCommentId)) 
                return new CommentPostResponse { Successful = false, StatusCode = 400, Message = "Parent comment not found." };
            dto.Rating = null;
        }

        if (dto.Rating.HasValue)
        {
            var alreadyRated = await _db.Comments.AnyAsync(c => c.RecipeId == dto.RecipeId && c.UserId == userId && c.Rating != null && c.IsApproved);
            if (alreadyRated) return new CommentPostResponse { Successful = false, StatusCode = 400, Message = "You have already rated this recipe." };
        }

        var commentDb = new Comment
        {
            Id = Guid.NewGuid(),
            RecipeId = dto.RecipeId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            LanguageCode = lang,
            Text = dto.Text,
            Rating = dto.Rating,
            ParentCommentId = dto.ParentCommentId,
        };

        _db.Comments.Add(commentDb);
        await _db.SaveChangesAsync();

        decimal newAvg = 0;
        int newCount = 0;

        var stats = await _db.Comments
            .Where(c => c.RecipeId == dto.RecipeId && c.Rating.HasValue && c.IsApproved)
            .GroupBy(c => c.RecipeId)
            .Select(g => new { Avg = g.Average(c => (decimal?)c.Rating) ?? 0, Count = g.Count() })
            .FirstOrDefaultAsync();

        if (stats != null)
        {
            var recipe = await _db.Recipes.FindAsync(dto.RecipeId);
            if (recipe != null)
            {
                recipe.Rating = stats.Avg;
                recipe.UsersRated = stats.Count;
                newAvg = stats.Avg;
                newCount = stats.Count;
                await _db.SaveChangesAsync();
            }
        }

        return new CommentPostResponse
        {
            Successful = true,
            StatusCode = 201,
            NewAverageRating = newAvg,
            NewUsersRatedCount = newCount,
            NewComment = new CommentDisplayDto
            {
                Id = commentDb.Id,
                Text = commentDb.Text ?? "",
                Rating = commentDb.Rating,
                CreatedAt = commentDb.CreatedAt ?? DateTime.UtcNow,
                UserName = dbUser.Name,
                UserSurname = dbUser.Surname ?? "",
                ParentCommentId = commentDb.ParentCommentId,
                Replies = new List<CommentDisplayDto>(),
                IsApproved = true,
                UserId = userId,
            }
        };
    }

    public async Task<CommentDeleteResponse> DeleteCommentAsync(Guid commentId, Guid userId)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null) return new CommentDeleteResponse { Successful = false, StatusCode = 404, Message = "Not found" };
        if (comment.UserId != userId) return new CommentDeleteResponse { Successful = false, StatusCode = 403, Message = "Forbidden" };

        comment.IsDeleted = true;
        comment.Rating = null;
        await _db.SaveChangesAsync();

        return await RecalculateRecipeRating(comment.RecipeId, userId);
    }

    public async Task<CommentDeleteResponse> UpdateCommentAsync(Guid commentId, Guid userId, CommentCreateDto dto)
    {
        var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (dbUser is null || !dbUser.CanComment) return new CommentDeleteResponse { Successful = false, StatusCode = 403, Message = "Forbidden" };

        var commentDb = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (commentDb is null) return new CommentDeleteResponse { Successful = false, StatusCode = 404, Message = "Not found" };
        if (commentDb.UserId != userId || !commentDb.IsApproved) return new CommentDeleteResponse { Successful = false, StatusCode = 403, Message = "Forbidden" };

        bool isRatingChanged = commentDb.Rating != dto.Rating;

        commentDb.IsEdited = true;
        commentDb.EditedAt = DateTime.UtcNow;
        commentDb.Text = dto.Text;
        commentDb.Rating = dto.Rating;
        await _db.SaveChangesAsync();

        if (isRatingChanged)
        {
            return await RecalculateRecipeRating(commentDb.RecipeId, userId);
        }
        
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == commentDb.RecipeId);
        return new CommentDeleteResponse
        {
            Successful = true,
            StatusCode = 200,
            UserAlreadyRated = await _db.Comments.AnyAsync(c => c.RecipeId == commentDb.RecipeId && c.UserId == userId && c.IsApproved && c.Rating.HasValue),
            NewAverageRating = recipe?.Rating ?? 0,
            NewUsersRatedCount = recipe?.UsersRated ?? 0
        };
    }

    public async Task<ToggleBoolResponse> AdminToggleApprovalAsync(Guid recipeId)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);
        if (recipe is null) return new ToggleBoolResponse { Successful = false, StatusCode = 404, Message = "Not found" };

        recipe.IsApproved = !recipe.IsApproved;
        await _db.SaveChangesAsync();
        return new ToggleBoolResponse { Successful = true, StatusCode = 200, State = recipe.IsApproved };
    }

    public async Task<ToggleBoolResponse> AdminToggleCheckedAsync(Guid recipeId)
    {
        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);
        if (recipe is null) return new ToggleBoolResponse { Successful = false, StatusCode = 404, Message = "Not found" };

        recipe.Checked = !recipe.Checked;
        await _db.SaveChangesAsync();
        return new ToggleBoolResponse { Successful = true, StatusCode = 200, State = recipe.Checked };
    }

    public async Task<List<AdminCommentDto>> AdminGetAllCommentsAsync(string lang)
    {
        return await _db.Comments.AsNoTracking()
            .Include(c => c.Recipe)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new AdminCommentDto
            {
                Id = c.Id,
                Text = c.Text ?? string.Empty,
                UserName = c.User.Name,
                UserSurname = c.User.Surname,
                CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                IsApproved = c.IsApproved,
                RecipeId = c.RecipeId,
                RecipeName = c.Recipe.Titles.GetValueOrDefault(lang) ?? c.Recipe.Titles.GetValueOrDefault("en") ?? "recipe title"
            }).ToListAsync();
    }

    public async Task<GeneralResponse> AdminToggleCommentApprovalAsync(Guid commentId)
    {
        var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment is null) return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Not found" };

        comment.IsApproved = !comment.IsApproved;

        if (comment.IsApproved && comment.Rating.HasValue)
        {
            var hasAnotherActiveRating = await _db.Comments.AnyAsync(c => c.UserId == comment.UserId && c.RecipeId == comment.RecipeId && c.Id != commentId && c.Rating.HasValue && c.IsApproved);
            if (hasAnotherActiveRating) comment.Rating = null;
        }

        await _db.SaveChangesAsync();
        await RecalculateRecipeRating(comment.RecipeId, comment.UserId);

        return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Status toggled" };
    }

    //Private

    private async Task<CommentDeleteResponse> RecalculateRecipeRating(Guid recipeId, Guid userId)
    {
        var ratings = await _db.Comments.AsNoTracking().Where(c => c.RecipeId == recipeId && c.Rating.HasValue && c.IsApproved).Select(c => (decimal)c.Rating!).ToListAsync();
        decimal newAvg = ratings.Any() ? ratings.Average() : 0;
        int newCount = ratings.Count;

        var recipe = await _db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);
        if (recipe != null)
        {
            recipe.Rating = newAvg;
            recipe.UsersRated = newCount;
            await _db.SaveChangesAsync();
        }

        return new CommentDeleteResponse
        {
            Successful = true,
            StatusCode = 200,
            UserAlreadyRated = await _db.Comments.AnyAsync(c => c.RecipeId == recipeId && c.UserId == userId && c.IsApproved && c.Rating.HasValue),
            NewAverageRating = newAvg,
            NewUsersRatedCount = newCount
        };
    }

    private async Task<(bool IsSuccess, string FileName, string Error)> ProcessAndSaveImageAsync(IFormFile imageFile, Guid? id)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension)) return (false, "", "Unsupported image format.");

        var uploadFolder = Path.Combine(_env.WebRootPath, "images", "recipes");
        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
        
        var fileName = id.HasValue ? $"{id}_{Guid.NewGuid()}.webp" : $"{Guid.NewGuid()}.webp";
        var filePath = Path.Combine(uploadFolder, fileName);

        try
        {
            using (var image = await Image.LoadAsync(imageFile.OpenReadStream()))
            {
                image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(1200, 0) }));
                await image.SaveAsWebpAsync(filePath, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder() { Quality = 80 });
            }
            return (true, fileName, "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process recipe image.");
            return (false, "", "Image Processing Error.");
        }
    }

    private static string GetMacrosLabel(decimal? Calories, decimal? Proteins, decimal? Fats, decimal? Carbohydrates)
    {
        if (!Calories.HasValue || !Proteins.HasValue || !Fats.HasValue || !Carbohydrates.HasValue) return "Unknown";

        var c = Calories.Value;
        var p = Proteins.Value;
        var f = Fats.Value;
        var cb = Carbohydrates.Value;

        if (c == 0) return "Light";
        if (c > 800 || (f > 30 && cb > 80)) return "Cheat Meal";
        if (p > 25 && f < 15) return "High Protein";
        if (cb < 20 && f > 10) return "Low Carb";
        if (cb > 60 && f < 10) return "Energy";
        return "Balanced";
    }
}