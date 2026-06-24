using DeepL;
using DeepL.Model;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.Shared.Enums;
using Pinula.Shared.Interface;
using Pinula.Shared.Models;
using Pinula.Shared.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text.Json;

namespace Pinula.API.Endpoints
{
    public static class RecipeEndpoint
    {

        public static void MapRecipeEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/recipes");

            //---------------------------------------------------------------Get previews filtered
            group.MapGet("/getPreviews/filtered", async (HttpRequest request,[AsParameters] RecipeFilterParameters filter, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/recipes/";
                var defaultImage = "default_recipe.png";
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                Guid? currentUserId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);

                var query = db.Recipes.AsNoTracking().AsQueryable();

                if (userDb is not null && userDb.Role == "admin" && filter.IncludeUnapproved)
                {

                }
                else
                {
                    query = query.Where(r => r.IsApproved);
                    query = query.Where(r => !r.IsDeleted);
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                    query = query.Where(r => r.Titles.GetValueOrDefault(languageCode).ToLower().Contains(filter.SearchTerm.ToLower()));

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

                if (filter.OnlyFavorites)
                {
                    if (currentUserId == null) return Results.Unauthorized();
                    query = query.Where(r => r.RecipeUsers.Any(ru => ru.UserId == currentUserId && ru.IsFavorite));
                }
                if (filter.OnlyMine)
                {
                    if (currentUserId == null) return Results.Unauthorized();
                    query = query.Where(r => r.UserId == currentUserId);
                }

                query = filter.Sort switch
                {
                    SortBy.Rating => filter.SortDescending
                        ? query.OrderByDescending(r => r.Rating)
                        : query.OrderBy(r => r.Rating),

                    SortBy.CookingTime => filter.SortDescending
                        ? query.OrderByDescending(r => r.CookingTime)
                        : query.OrderBy(r => r.CookingTime),

                    SortBy.Calories => filter.SortDescending
                        ? query.OrderByDescending(r => r.Calories)
                        : query.OrderBy(r => r.Calories),

                    SortBy.Oldest => query.OrderBy(r => r.RecipeCreated),

                    _ => query.OrderByDescending(r => r.RecipeCreated)
                };

                var results = await query
                    .Skip(filter.Skip)
                    .Take(filter.Amount)
                    .Select(r => new RecipePreviewDto
                    {
                        Id = r.Id,
                        Title = r.Titles.GetValueOrDefault(languageCode) ?? r.Titles.GetValueOrDefault("en") ?? "Title",
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
                    })
                    .ToListAsync();

                return Results.Ok(results);
            });

            //---------------------------------------------------------------Toggle favorite recipe
            group.MapPost("/toggleFavorite/{recipeId:guid}", async (Guid recipeId, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var recipeExists = await db.Recipes.AnyAsync(r => r.Id == recipeId);
                if (!recipeExists)
                {
                    return Results.NotFound(new { message = "Recipe does not exist" });
                }
                var favorite = await db.RecipeUsers.FirstOrDefaultAsync(ru => ru.RecipeId == recipeId && ru.UserId == userId);

                if (favorite == null)
                {
                    db.RecipeUsers.Add(new RecipeUser()
                    {
                        RecipeId = recipeId,
                        UserId = userId,
                        IsFavorite = true
                    });
                    await db.SaveChangesAsync();
                    return Results.Ok(new { isFavorite = true });
                }
                else
                {
                    favorite.IsFavorite = !favorite.IsFavorite;
                    await db.SaveChangesAsync();
                    return Results.Ok(new { isFavorite = favorite.IsFavorite });
                }
            }).RequireAuthorization();


            //---------------------------------------------------------------Get recipe details
            group.MapGet("/getRecipeDetails/{recipeId:guid}", async (HttpRequest request, Guid recipeId, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/recipes/";
                var defaultImage = "default_recipe.png";
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                Guid? currentUserId = user.GetUserId();

                var recipe = await db.Recipes.AsNoTracking().Where(r => r.Id == recipeId).Select(r => new RecipeDetailsDto()
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    OriginalLanguage = languageCode,
                    Title = r.Titles.GetValueOrDefault(languageCode) ?? r.Titles.GetValueOrDefault("en") ?? "Title",
                    PhotoUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(r.PhotoUrl) ? defaultImage : r.PhotoUrl)}",
                    CookingTime = r.CookingTime,
                    ServingsAmount = r.ServingsAmount,
                    Difficulty = (DifficultyLevel)r.Difficulty,
                    Calories = r.Calories,
                    Proteins = r.Proteins,
                    Fats = r.Fats,
                    Carbohydrates = r.Carbohydrates,
                    Fiber = r.Fiber,
                    RecipeCreated = r.RecipeCreated,
                    Rating = r.Rating,
                    UsersRated = r.UsersRated,
                    RecipeIngredients = r.RecipeIngredients.Select(ri => new RecipeIngredientDetailDto
                    {
                        Quantity = ri.Quantity ?? 0,
                        IngredientName = ri.Ingredient.Names.GetValueOrDefault(languageCode) ?? ri.Ingredient.Names.GetValueOrDefault("en") ?? "Ingredient",
                        UnitName = ri.Unit.Names.GetValueOrDefault(languageCode) ?? ri.Unit.Names.GetValueOrDefault("en") ?? "Unit",
                        IngredientId = ri.Ingredient.Id,
                        UnitId = ri.Unit.Id,
                    }).ToList(),
                    RecipeSteps = r.RecipeSteps.Select(rs => new RecipeStepDisplayDto
                    {
                        Id = rs.Id,
                        RecipeId = rs.Id,
                        StepNumber = rs.StepNumber,
                        Description = rs.Descriptions.GetValueOrDefault(languageCode) ?? rs.Descriptions.GetValueOrDefault("en") ?? "Step description",
                    }).ToList(),
                    ServingUnit = new UnitPreviewDto() { Name = r.ServingUnit.Names.GetValueOrDefault(languageCode) ?? r.ServingUnit.Names.GetValueOrDefault("en") ?? "UnitName", Id = r.ServingUnit.Id },
                    UserName = r.User.Name,
                    UserSurname = r.User.Surname,
                    Categories = r.Categories.Select(c => new CategoryDisplayDto
                    {
                        Id = c.Id,
                        SortOrder = c.SortOrder,
                        Name = c.Names.GetValueOrDefault(languageCode) ?? c.Names.GetValueOrDefault("en") ?? "Category",
                        PictureUrl = c.PictureUrl,
                        ParentCategoryId = c.ParentCategoryId,
                    }).ToList(),

                    IsFavorite = currentUserId != null && r.RecipeUsers.Any(ru => ru.UserId == currentUserId && ru.IsFavorite),
                    UserAlreadyRated = currentUserId != Guid.Empty && db.Comments.Any(c => c.RecipeId == r.Id && c.UserId == currentUserId && c.Rating != null && c.IsApproved)

                }).FirstOrDefaultAsync();

                if (recipe == null) return Results.NotFound();

                var allComments = await db.Comments.AsNoTracking().Where(c => c.RecipeId == recipeId).Select(c => new CommentPreview
                {
                    Id = c.Id,
                    Text = c.Text??string.Empty,
                    Rating = c.Rating,
                    CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                    UserId =c.User.Id,
                    UserName = c.User.Name,
                    UserSurname = c.User.Surname,
                    IsApproved = c.IsApproved,
                    IsEdited = c.IsEdited,
                    IsDeleted = c.IsDeleted,
                    EditedAt = c.EditedAt,
                    ParentCommentId = c.ParentCommentId,
                    Replies = new List<CommentPreview>()
                }).OrderByDescending(c => c.CreatedAt).ToListAsync();

                var commentLookup = allComments.ToDictionary(c => c.Id);
                var rootComments = new List<CommentPreview>();

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

                //rootComments.RemoveAll(rc => !rc.Replies.Any() && !rc.IsApproved);

                recipe.Comments = rootComments;

                return Results.Ok(recipe);
            });


            //---------------------------------------------------------------Create recipe
            group.MapPost("/create", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db, IWebHostEnvironment env, ITranslationService translationService) =>
            {
                var userId = user.GetUserId();
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (dbUser is null) return Results.NotFound("User not found.");
                if (!dbUser.CanCreateRecipes)
                {
                    return Results.Forbid();
                }

                var form = await request.ReadFormAsync();

                var dtoStr = form["recipeData"];
                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Missing recipe data.");

                var dto = JsonSerializer.Deserialize<RecipeCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Invalid recipe data.");
            
                string finalPhotoUrl = "default_recipe_picture.png";
                var file = form.Files.GetFile("image");

                if (file is { Length: > 0 })
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                        return Results.BadRequest("Unsupported image format.");

                    var uploadFolder = Path.Combine(env.WebRootPath, "images", "recipes");
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

                        finalPhotoUrl = fileName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Image Processing Error: {ex.Message}");
                    }
                }

                var ingredientIds = dto.RecipeIngredients.Select(x => x.IngredientId).ToList();
                var dbIngredients = await db.Ingredients.Include(x => x.IngredientUnits).Where(x => ingredientIds.Contains(x.Id)).ToListAsync();

                var dbCategories = await db.Categories.Where(x => dto.CategoriesIds.Contains(x.Id)).ToListAsync();

                string targetLanguage = languageCode == "en" ? "cs" : "en";

                var titleTranslated = await translationService.TranslateTextAsync(dto.Title, targetLanguage) ?? dto.Title;
                var titles = new Dictionary<string, string> { { languageCode, dto.Title }, { targetLanguage, titleTranslated } };


                var newRecipe = new Recipe()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OriginalLanguage = languageCode,
                    Titles = titles,
                    PhotoUrl = finalPhotoUrl,
                    CookingTime = dto.CookingTime,
                    ServingsAmount = dto.ServingsAmount,
                    ServingUnitId = dto.ServingUnit,
                    Difficulty = dto.Difficulty,
                    RecipeCreated = DateTime.UtcNow,
                    Calories = 0,
                    Proteins = 0,
                    Fats = 0,
                    Carbohydrates = 0,
                    Fiber = 0,
                    Rating = (decimal)0,
                    UsersRated = 0,

                };

                foreach (var i in dto.RecipeIngredients)
                {
                    var dbIng = dbIngredients.FirstOrDefault(x => x.Id == i.IngredientId);

                    if (dbIng != null)
                    {
                        var ingredientUnit = dbIng.IngredientUnits.FirstOrDefault(iu => iu.UnitId == i.UnitId);

                        decimal conversionFactor = ingredientUnit?.AmountInGrams ?? 1;
                        decimal factor = (conversionFactor / 100) * (i.Quantity / dto.ServingsAmount) ?? 0;

                        newRecipe.Calories += factor * dbIng.Calories;
                        newRecipe.Proteins += factor * dbIng.Proteins;
                        newRecipe.Fats += factor * dbIng.Fats;
                        newRecipe.Carbohydrates += factor * dbIng.Carbohydrates;
                        newRecipe.Fiber += factor * dbIng.Fiber;
                    }


                    newRecipe.RecipeIngredients.Add(new RecipeIngredient
                    {

                        RecipeId = newRecipe.Id,
                        IngredientId = i.IngredientId,
                        Quantity = i.Quantity,
                        UnitId = i.UnitId,
                        ConversionFactor = i.ConversionFactor
                    });
                }

                foreach (var step in dto.RecipeSteps)
                {
                    var descriptionTranslated = await translationService.TranslateTextAsync(step.Description, targetLanguage) ?? step.Description;
                    var descriptions = new Dictionary<string, string> { { languageCode, step.Description }, { targetLanguage, descriptionTranslated } };

                    newRecipe.RecipeSteps.Add(new RecipeStep
                    {
                        Id = Guid.NewGuid(),
                        RecipeId = newRecipe.Id,
                        Descriptions = descriptions,
                        StepNumber = step.StepNumber
                    });
                }

                foreach (var category in dbCategories)
                {
                    newRecipe.Categories.Add(category);
                }

                db.Recipes.Add(newRecipe);
                await db.SaveChangesAsync();

                return Results.Ok(newRecipe.Id);
            }).RequireAuthorization();



            //---------------------------------------------------------------Update recipe
            group.MapPut("/update/{id:guid}", async (Guid id, HttpRequest request, ClaimsPrincipal user, PinulaDbContext db, IWebHostEnvironment env, ITranslationService translationService) =>
            {
                var userId = user.GetUserId();
                var existingRecipe = await db.Recipes
                    .Include(r => r.Categories)
                    .FirstOrDefaultAsync(r => r.Id == id);
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                if (existingRecipe is null) return Results.NotFound("Recipe not found.");
                if (existingRecipe.UserId != userId) return Results.Forbid();

                var form = await request.ReadFormAsync();
                var dtoStr = form["recipeData"];
                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Missing recipe data.");

                var dto = JsonSerializer.Deserialize<RecipeCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Invalid recipe data.");

                var file = form.Files.GetFile("image");
                if (file is { Length: > 0 })
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension)) return Results.BadRequest("Unsupported image format.");

                    var uploadFolder = Path.Combine(env.WebRootPath, "images", "recipes");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    try
                    {
                        using (var image = await Image.LoadAsync(file.OpenReadStream()))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(1200, 0) }));
                            await image.SaveAsJpegAsync(filePath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 80 });
                        }
                        existingRecipe.PhotoUrl = fileName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Image Processing Error: {ex.Message}");
                    }
                }

                try
                {
                    await db.RecipeIngredients.Where(ri => ri.RecipeId == id).ExecuteDeleteAsync();

                    var oldSteps = await db.RecipeSteps.Where(rs => rs.RecipeId == id).ToListAsync();
                    await db.RecipeSteps.Where(rs => rs.RecipeId == id).ExecuteDeleteAsync();

                    string targetLanguage = languageCode == "en" ? "cs" : "en";

                    if (existingRecipe.Titles.GetValueOrDefault(languageCode) != dto.Title)
                    {
                        var titleTranslated = await translationService.TranslateTextAsync(dto.Title, targetLanguage) ?? dto.Title;
                        existingRecipe.Titles[languageCode] = dto.Title;
                        existingRecipe.Titles[targetLanguage] = titleTranslated;
                    }

                    existingRecipe.CookingTime = dto.CookingTime;
                    existingRecipe.ServingsAmount = dto.ServingsAmount;
                    existingRecipe.ServingUnitId = dto.ServingUnit;
                    existingRecipe.Difficulty = dto.Difficulty;

                    existingRecipe.Calories = 0;
                    existingRecipe.Proteins = 0;
                    existingRecipe.Fats = 0;
                    existingRecipe.Carbohydrates = 0;
                    existingRecipe.Fiber = 0;

                    var newCategoryIds = dto.CategoriesIds;

                    var categoriesToRemove = existingRecipe.Categories.Where(c => !newCategoryIds.Contains(c.Id)).ToList();
                    foreach (var c in categoriesToRemove)
                    {
                        existingRecipe.Categories.Remove(c);
                    }

                    var existingCatIds = existingRecipe.Categories.Select(c => c.Id).ToList();
                    var categoriesToAddIds = newCategoryIds.Except(existingCatIds).ToList();

                    if (categoriesToAddIds.Any())
                    {
                        var catsToAdd = await db.Categories.Where(c => categoriesToAddIds.Contains(c.Id)).ToListAsync();
                        foreach (var c in catsToAdd)
                        {
                            existingRecipe.Categories.Add(c);
                        }
                    }

                    var ingredientIds = dto.RecipeIngredients.Select(x => x.IngredientId).ToList();
                    var dbIngredients = await db.Ingredients.Include(x => x.IngredientUnits).Where(x => ingredientIds.Contains(x.Id)).ToListAsync();

                    var newIngredients = new List<RecipeIngredient>();

                    foreach (var i in dto.RecipeIngredients)
                    {
                        var dbIng = dbIngredients.FirstOrDefault(x => x.Id == i.IngredientId);

                        if (dbIng != null)
                        {
                            var ingredientUnit = dbIng.IngredientUnits.FirstOrDefault(iu => iu.UnitId == i.UnitId);
                            decimal conversionFactor = ingredientUnit?.AmountInGrams ?? 1;
                            decimal factor = (conversionFactor / 100) * (i.Quantity / dto.ServingsAmount) ?? 0;

                            existingRecipe.Calories += factor * dbIng.Calories;
                            existingRecipe.Proteins += factor * dbIng.Proteins;
                            existingRecipe.Fats += factor * dbIng.Fats;
                            existingRecipe.Carbohydrates += factor * dbIng.Carbohydrates;
                            existingRecipe.Fiber += factor * dbIng.Fiber;
                        }

                        newIngredients.Add(new RecipeIngredient
                        {
                            RecipeId = existingRecipe.Id,
                            IngredientId = i.IngredientId,
                            Quantity = i.Quantity,
                            UnitId = i.UnitId,
                            ConversionFactor = i.ConversionFactor
                        });
                    }
                    db.RecipeIngredients.AddRange(newIngredients);


                    var newSteps = new List<RecipeStep>();
                    foreach (var step in dto.RecipeSteps)
                    {
                        var newStep = new RecipeStep()
                        {
                            Id = Guid.NewGuid(),
                            RecipeId = existingRecipe.Id,
                            StepNumber = step.StepNumber,
                            Descriptions = new Dictionary<string, string>()
                        };


                        var oldStep = oldSteps.FirstOrDefault(s => s.StepNumber == step.StepNumber);

                        if (oldStep != null)
                        {
                            if (step.Description != oldStep.Descriptions.GetValueOrDefault(languageCode))
                            {
                                var descriptionTranslated = await translationService.TranslateTextAsync(step.Description, targetLanguage) ?? step.Description;
                                newStep.Descriptions[languageCode] = step.Description;
                                newStep.Descriptions[targetLanguage] = descriptionTranslated;
                            }
                            else
                            {
                                newStep.Descriptions = oldStep.Descriptions;
                            }
                        }
                        else
                        {
                            var descriptionTranslated = await translationService.TranslateTextAsync(step.Description, targetLanguage) ?? step.Description;
                            newStep.Descriptions[languageCode] = step.Description;
                            newStep.Descriptions[targetLanguage] = descriptionTranslated;
                        }
                        newSteps.Add(newStep);
                    }
                    db.RecipeSteps.AddRange(newSteps);

                    await db.SaveChangesAsync();

                    return Results.Ok(existingRecipe.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("================ UPDATE RECIPE ERROR ================");
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine("=====================================================");

                    return Results.Problem("An error occurred while updating the recipe.");
                }
            }).RequireAuthorization();


            //---------------------------------------------------------------Post comment
            group.MapPost("/postComment", async (Comment comment, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (dbUser is null) return Results.NotFound("User not found.");
                if (!dbUser.CanComment)
                {
                    return Results.Forbid();
                }
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var exists = await db.Recipes.AnyAsync(r => r.Id == comment.RecipeId);
                bool isNewRating = comment.Rating.HasValue;

                if (comment.ParentCommentId.HasValue)
                {
                    var parentExists = await db.Comments.AnyAsync(c => c.Id == comment.ParentCommentId);
                    if (!parentExists) return Results.BadRequest("Parent comment not found.");
                    comment.Rating = null;
                }

                if (comment.Rating.HasValue)
                {
                    var alreadyRated = comment.Rating.HasValue && await db.Comments.AnyAsync(c => c.RecipeId == comment.RecipeId && c.UserId == userId && c.Rating != null && c.IsApproved);
                    if (alreadyRated) return Results.BadRequest("You have already rated this recipe.");
                }

                comment.UserId = userId;
                comment.CreatedAt = DateTime.UtcNow;
                comment.LanguageCode = languageCode;
                comment.IsApproved = true;
                db.Comments.Add(comment);
                await db.SaveChangesAsync();

                decimal newAvg = 0;
                int newCount = 0;

                var stats = await db.Comments
                    .Where(c => c.RecipeId == comment.RecipeId && c.Rating.HasValue && comment.IsApproved)
                    .GroupBy(c => c.RecipeId)
                    .Select(g => new { Avg = g.Average(c => (decimal?)c.Rating) ?? 0, Count = g.Count() })
                    .FirstOrDefaultAsync();

                if (stats != null)
                {
                    var recipe = await db.Recipes.FindAsync(comment.RecipeId);
                    if (recipe != null)
                    {
                        recipe.Rating = stats.Avg;
                        recipe.UsersRated = stats.Count;
                        newAvg = stats.Avg;
                        newCount = stats.Count;
                        await db.SaveChangesAsync();
                    }
                }

                var userProfile = await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Name, u.Surname })
                    .FirstOrDefaultAsync();

                var response = new PostCommentResponse
                {
                    NewAverageRating = newAvg,
                    NewUsersRatedCount = newCount,
                    NewComment = new CommentPreview
                    {
                        Id = comment.Id,
                        Text = comment.Text ?? "",
                        Rating = comment.Rating,
                        CreatedAt = comment.CreatedAt ?? DateTime.UtcNow,
                        UserName = userProfile?.Name ?? "User",
                        UserSurname = userProfile?.Surname ?? "",
                        ParentCommentId = comment.ParentCommentId,
                        Replies = new List<CommentPreview>(),
                        IsApproved = true,
                        UserId = userId,
                    }
                };

                return Results.Ok(response);

            }).RequireAuthorization();

            //---------------------------------------------------------------Delete comment
            group.MapDelete("/deleteComment/{commentId:guid}", async (Guid commentId, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();

                var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
                if (comment == null) return Results.NotFound();
                if (comment.UserId != userId) return Results.Unauthorized();

                comment.IsDeleted = true;
                comment.Rating = null;

                var recipeId = comment.RecipeId;

                var ratings = await db.Comments.AsNoTracking().Where(c => c.RecipeId == recipeId && c.Rating.HasValue && c.IsApproved).Select(c => (decimal)c.Rating!).ToListAsync();

                decimal newAvg = 0;
                int newCount = ratings.Count;

                if (newCount > 0)
                {
                    newAvg = ratings.Average();
                }

                var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);
                if (recipe != null)
                {
                    recipe.Rating = newAvg;
                    recipe.UsersRated = newCount;
                }
                await db.SaveChangesAsync();



                return Results.Ok(new DeleteCommentResponse
                {
                    UserAlreadyRated = await db.Comments.AnyAsync(c => c.RecipeId == recipeId && c.UserId == userId && c.IsApproved && c.Rating.HasValue),
                    NewAverageRating = newAvg,
                    NewUsersRatedCount = newCount
                });

            }).RequireAuthorization();

            //---------------------------------------------------------------Update comment
            group.MapPut("/updateComment/{commentId:guid}", async (Comment comment, Guid commentId, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (dbUser is null) return Results.NotFound("User not found.");
                if (!dbUser.CanComment) return Results.Forbid();

                if (comment is null) return Results.BadRequest();
                var commentDb = await db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
                if (commentDb is null) return Results.NotFound();
                if (commentDb.UserId != userId) return Results.Unauthorized();
                if (!comment.IsApproved) return Results.Forbid();

                bool isRatingChanged = commentDb.Rating != comment.Rating;

                commentDb.IsEdited = true;
                commentDb.EditedAt = DateTime.UtcNow;
                commentDb.Text = comment.Text;
                commentDb.Rating = comment.Rating;

                decimal newAvg = 0;
                int newCount = 0;

                var recipeId = commentDb.RecipeId;

                if (isRatingChanged)
                {
                    await db.SaveChangesAsync();

                    var ratings = await db.Comments.AsNoTracking().Where(c => c.RecipeId == recipeId && c.Rating.HasValue && c.IsApproved).Select(c => (decimal)c.Rating!).ToListAsync();

                    newCount = ratings.Count;
                    if (newCount > 0)
                    {
                        newAvg = ratings.Average();
                    }

                    var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);
                    if (recipe != null)
                    {
                        recipe.Rating = newAvg;
                        recipe.UsersRated = newCount;
                    }
                }
                else
                {
                    var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);
                    if (recipe != null)
                    {
                        newAvg = recipe.Rating ?? 0;
                        newCount = recipe.UsersRated ?? 0;
                    }
                }

                await db.SaveChangesAsync();

                return Results.Ok(new DeleteCommentResponse
                {
                    UserAlreadyRated = await db.Comments.AnyAsync(c => c.RecipeId == recipeId && c.UserId == userId && c.IsApproved && c.Rating.HasValue),
                    NewAverageRating = newAvg,
                    NewUsersRatedCount = newCount
                });

            }).RequireAuthorization();

            //---------------------------------------------------------------Toggle recipe approval
            group.MapPost("/admin/toggleApproval/{recipeId:guid}", async (Guid recipeId, PinulaDbContext db) =>
            {
                var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);
                if (recipe is null) return Results.NotFound("Recipe not found");

                recipe.IsApproved = !recipe.IsApproved;

                await db.SaveChangesAsync();
                return Results.Ok(new { isApproved = recipe.IsApproved });

            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Get all comments
            group.MapGet("/admin/allComments", async (PinulaDbContext db) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var comments = await db.Comments
                    .Include(c => c.Recipe)
                    .Include(c => c.User)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new AdminCommentDto
                    {
                        Id = c.Id,
                        Text = c.Text??string.Empty,
                        UserName = c.User.Name,
                        UserSurname = c.User.Surname,
                        CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                        IsApproved = c.IsApproved,
                        RecipeId = c.RecipeId,
                        RecipeName = c.Recipe.Titles.GetValueOrDefault(languageCode) ?? c.Recipe.Titles.GetValueOrDefault("en") ?? "recipe title"
                    })
                    .ToListAsync();

                return Results.Ok(comments);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Toggle comment approval
            group.MapPost("/admin/toggleCommentApproval/{commentId:guid}", async (Guid commentId, PinulaDbContext db) =>
            {
                var comment = await db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
                if (comment is null) return Results.NotFound("Comment not found.");

                comment.IsApproved = !comment.IsApproved;

                if (comment.IsApproved && comment.Rating.HasValue)
                {
                    var hasAnotherActiveRating = await db.Comments.AnyAsync(c =>
                        c.UserId == comment.UserId &&
                        c.RecipeId == comment.RecipeId &&
                        c.Id != commentId &&
                        c.Rating.HasValue &&
                        c.IsApproved);

                    if (hasAnotherActiveRating)
                    {
                        comment.Rating = null;
                    }
                }

                await db.SaveChangesAsync();

                var recipeId = comment.RecipeId;
                var stats = await db.Comments
                    .Where(c => c.RecipeId == recipeId && c.Rating != null && c.IsApproved)
                    .GroupBy(c => c.RecipeId)
                    .Select(g => new { Avg = g.Average(c => (decimal?)c.Rating) ?? 0, Count = g.Count() })
                    .FirstOrDefaultAsync();

                decimal newAvg = stats?.Avg ?? 0;
                int newCount = stats?.Count ?? 0;

                var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);
                if (recipe != null)
                {
                    recipe.Rating = newAvg;
                    recipe.UsersRated = newCount;
                    await db.SaveChangesAsync();
                }

                return Results.Ok();
            }).RequireAuthorization("AdminOnly");


            //---------------------------------------------------------------Delete recipe
            group.MapDelete("/delete/{recipeId:guid}", async (Guid recipeId, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();

                var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == recipeId);

                if (recipe is null) return Results.NotFound("Recipe not found");
                if (recipe.UserId != userId) return Results.Unauthorized();

                recipe.IsDeleted = true;

                await db.SaveChangesAsync();
                return Results.Ok();
            });


        }

        public static string GetMacrosLabel(decimal? Calories, decimal? Proteins, decimal? Fats, decimal? Carbohydrates)
        {
            if (!Calories.HasValue || !Proteins.HasValue || !Fats.HasValue || !Carbohydrates.HasValue)
            {
                return "Unknown";
            }

            var calories = Calories.Value;
            var proteins = Proteins.Value;
            var fats = Fats.Value;
            var carbs = Carbohydrates.Value;

            if (calories == 0) return "Light";

            if (calories > 800 || (fats > 30 && carbs > 80))
            {
                return "Cheat Meal";
            }
            if (proteins > 25 && fats < 15)
            {
                return "High Protein";
            }

            if (carbs < 20 && fats > 10)
            {
                return "Low Carb";
            }

            if (carbs > 60 && fats < 10)
            {
                return "Energy";
            }

            return "Balanced";
        }
    } 
}
