using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text.Json;
using Pinula.Shared.Enums;

namespace Pinula.API.Endpoints
{
    public static class RecipeEndpoint
    {

        public static void MapRecipeEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/recipes");

            //---------------------------------------------------------------Get recipes
            group.MapGet("/get", async (int? amount, PinulaDbContext db) =>
            {
                var query = db.Recipes.Include(r => r.User).OrderByDescending(r => r.RecipeCreated);

                if (amount != 0 && amount != null)
                {
                    return await query.Take(amount.Value).ToListAsync();
                }

                return await query.ToListAsync();
            });

            //---------------------------------------------------------------Get previews
            group.MapGet("/getPreviews", async (HttpRequest request, int? amount, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/recipes/";
                var defaultImage = "default_recipe.png";

                var query = db.Recipes
                    .OrderByDescending(r => r.RecipeCreated)
                    .AsNoTracking()
                    .Select(r => new RecipePreviewDto
                    {
                        Id = r.Id,
                        Title = r.Title,
                        PhotoUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(r.PhotoUrl) ? defaultImage : r.PhotoUrl)}",
                        CookingTime = r.CookingTime,
                        ServingsAmount = r.ServingsAmount,
                        Difficulty = r.Difficulty,
                        Rating = r.Rating,
                        UserName = r.User.Name,
                        Calories = r.Calories,
                    });

                if (amount > 0 && amount != null)
                {
                    return await query.Take(amount.Value).ToListAsync();
                }
                else
                {
                    return await query.ToListAsync();
                }
            });

            //---------------------------------------------------------------Get previews filtered
            group.MapGet("/getPreviews/filtered", async (HttpRequest request,[AsParameters] RecipeFilterParameters filter, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/recipes/";
                var defaultImage = "default_recipe.png";

                Guid? currentUserId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);

                var query = db.Recipes.AsNoTracking().AsQueryable();

                if (userDb is not null && userDb.Role == "admin" && filter.IncludeUnapproved)
                {

                }
                else
                {
                    query = query.Where(r => r.IsApproved);
                }

                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                    query = query.Where(r => r.Title.ToLower().Contains(filter.SearchTerm.ToLower()));

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
                        Title = r.Title,
                        PhotoUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(r.PhotoUrl) ? defaultImage : r.PhotoUrl)}",
                        CookingTime = r.CookingTime,
                        Difficulty = r.Difficulty,
                        Rating = r.Rating,
                        UserName = r.User.Name,
                        Calories = r.Calories,
                        ServingsAmount = r.ServingsAmount,
                        IsFavorite = currentUserId != null && r.RecipeUsers.Any(ru => ru.UserId == currentUserId && ru.IsFavorite),
                        IsApproved = r.IsApproved
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
                Guid? currentUserId = user.GetUserId();

                var recipe = await db.Recipes.AsNoTracking().Where(r => r.Id == recipeId).Select(r => new RecipeDetailsDto()
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Title = r.Title,
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
                        IngredientName = ri.Ingredient.Name,
                        UnitName = ri.Unit.Name
                    }).ToList(),
                    RecipeSteps = r.RecipeSteps,
                    ServingUnit = r.ServingUnit,
                    UserName = r.User.Name,
                    UserSurname = r.User.Surname,
                    Categories = r.Categories,

                    IsFavorite = currentUserId != null && r.RecipeUsers.Any(ru => ru.UserId == currentUserId && ru.IsFavorite)
                }).FirstOrDefaultAsync();

                if (recipe == null) return Results.NotFound();

                var allComments = await db.Comments.AsNoTracking().Where(c => c.RecipeId == recipeId).Select(c => new CommentPreview
                {
                    Id = c.Id,
                    Text = c.Text??string.Empty,
                    Rating = c.Rating,
                    CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                    UserName = c.User.Name,
                    UserSurname = c.User.Surname,
                    IsApproved = c.IsApproved,
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
            group.MapPost("/create", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db, IWebHostEnvironment env) =>
            {
                var userId = user.GetUserId();

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

                var newRecipe = new Recipe()
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = dto.Title,
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

                        decimal conversionFactor = ingredientUnit?.ToDefaultUnit ?? 1;
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
                    newRecipe.RecipeSteps.Add(new RecipeStep
                    {
                        Id = Guid.NewGuid(),
                        RecipeId = newRecipe.Id,
                        Description = step.Description,
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

                var exists = await db.Recipes.AnyAsync(r => r.Id == comment.RecipeId);
                bool isNewRating = comment.Rating.HasValue;

                if (comment.ParentCommentId.HasValue)
                {
                    var parentExists = await db.Comments.AnyAsync(c => c.Id == comment.ParentCommentId);
                    if (!parentExists) return Results.BadRequest("Parent comment not found.");
                    comment.Rating = null;
                }
                else
                {
                    var alreadyRated = comment.Rating.HasValue && await db.Comments.AnyAsync(c => c.RecipeId == comment.RecipeId && c.UserId == userId && c.Rating != null);
                    if (alreadyRated) return Results.BadRequest("You have already rated this recipe.");
                }

                comment.UserId = userId;
                comment.CreatedAt = DateTime.UtcNow;
                comment.IsApproved = true;
                db.Comments.Add(comment);
                await db.SaveChangesAsync();

                decimal newAvg = 0;
                int newCount = 0;

                var stats = await db.Comments
                    .Where(c => c.RecipeId == comment.RecipeId && c.Rating != null)
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
                        Text = comment.Text ?? "",
                        Rating = comment.Rating,
                        CreatedAt = comment.CreatedAt ?? DateTime.UtcNow,
                        UserName = userProfile?.Name ?? "User",
                        UserSurname = userProfile?.Surname ?? "",
                        ParentCommentId = comment.ParentCommentId,
                        Replies = new List<CommentPreview>(),
                        IsApproved = true
                    }
                };

                return Results.Ok(response);

            }).RequireAuthorization();


            //---------------------------------------------------------------Get user comment
            /*
            group.MapGet("/getUserComment/{recipeId:guid}", async (Guid recipeId, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();

                var comResponse = await db.Comments.Include(c => c.User).AsNoTracking().Where(c => c.RecipeId == recipeId && c.UserId == userId).Select(c => new PostCommentResponse
                    {
                        RecipeId = c.RecipeId,
                        UserId = c.UserId,
                        Text = c.Text,
                        Rating = c.Rating,
                        UserName = c.User.Name,
                        UserSurname = c.User.Surname,
                        CreatedAt = c.CreatedAt ?? DateTime.UtcNow,
                    }).FirstOrDefaultAsync();

                if (comResponse == null) return Results.NotFound();

                return Results.Ok(comResponse);
            }).RequireAuthorization();
            */


            //---------------------------------------------------------------Remove user comment
            group.MapDelete("/deleteComment/{recipeId:guid}", async (Guid recipeId, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();

                var comment = await db.Comments.FirstOrDefaultAsync(c => c.RecipeId == recipeId && c.UserId == userId);
                if (comment == null) return Results.NotFound();

                db.Comments.Remove(comment);
                await db.SaveChangesAsync();

                var ratings = await db.Comments.AsNoTracking().Where(c => c.RecipeId == recipeId && c.Rating.HasValue).Select(c => (decimal)c.Rating!).ToListAsync();

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
                    await db.SaveChangesAsync();
                }

                return Results.Ok(new DeleteCommentResponse
                {
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
                        RecipeName = c.Recipe.Title
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
                await db.SaveChangesAsync();

                return Results.Ok(new { isApproved = comment.IsApproved });
            }).RequireAuthorization("AdminOnly");


        }
    }
}
