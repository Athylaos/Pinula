using DeepL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace Pinula.API.Endpoints
{
    public static class CategoryEndpoints
    {
        public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/categories");
            //---------------------------------------------------------------Get all categories
            group.MapGet("/getAll", async (HttpRequest request, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/categories/";
                var defaultImage = "default_category.png";
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var allCategories = await db.Categories.AsNoTracking().ToListAsync();

                var rootCategories = allCategories.Where(c => c.ParentCategoryId == null).OrderBy(c => c.SortOrder).ToList();

                List<CategoryDisplayDto> BuildCategoryTree(List<Category> currentLevelItems)
                {
                    var resultList = new List<CategoryDisplayDto>();

                    foreach (var cat in currentLevelItems)
                    {
                        var dto = new CategoryDisplayDto()
                        {
                            Id = cat.Id,
                            ParentCategoryId = cat.ParentCategoryId,
                            Name = cat.Names.GetValueOrDefault(languageCode) ?? cat.Names.GetValueOrDefault("en") ?? "Category",
                            PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(cat.PictureUrl) ? defaultImage : cat.PictureUrl)}",
                            SortOrder = cat.SortOrder,
                            ChildCategories = BuildCategoryTree(allCategories.Where(sub => sub.ParentCategoryId == cat.Id).OrderBy(sub => sub.SortOrder).ToList())
                        };

                        resultList.Add(dto);
                    }

                    return resultList;
                }

                var finalTree = BuildCategoryTree(rootCategories);
                return Results.Ok(finalTree);
            });


            //---------------------------------------------------------------Get main categories
            group.MapGet("/getMain", async (HttpRequest request, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/categories/";
                var defaultImage = "default_category.png";
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                return await db.Categories.AsNoTracking().Where(c => c.ParentCategoryId == null).Include(c => c.ChildCategories).OrderBy(c => c.SortOrder).Select(category => new CategoryDisplayDto()
                {
                    Id = category.Id,
                    ParentCategoryId = category.ParentCategoryId,
                    Name = category.Names.GetValueOrDefault(languageCode) ?? category.Names.GetValueOrDefault("en") ?? "Category",
                    PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(category.PictureUrl) ? defaultImage : category.PictureUrl)}",
                    SortOrder = category.SortOrder,
                    ChildCategories = category.ChildCategories
                        .OrderBy(sub => sub.SortOrder)
                        .Select(sub => new CategoryDisplayDto()
                        {
                            Id = sub.Id,
                            ParentCategoryId = sub.ParentCategoryId,
                            Name = sub.Names.GetValueOrDefault(languageCode) ?? sub.Names.GetValueOrDefault("en") ?? "Subcategory",
                            PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(sub.PictureUrl) ? defaultImage : sub.PictureUrl)}",
                            SortOrder = sub.SortOrder,
                            ChildCategories = new List<CategoryDisplayDto>()
                        }).ToList()
                }).ToListAsync();
            });

            //---------------------------------------------------------------Get category
            group.MapGet("/get/{categoryId:guid}", async (HttpRequest request, Guid categoryId, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/categories/";
                var defaultImage = "default_category.png";
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                var category = await db.Categories.AsNoTracking().Include(c => c.ChildCategories).FirstOrDefaultAsync(c => c.Id == categoryId);

                if (category is null) return Results.NotFound("Category not found");

                var categoryDto = new CategoryDisplayDto()
                {
                    Id = category.Id,
                    ParentCategoryId = category.ParentCategoryId,
                    Name = category.Names.GetValueOrDefault(languageCode) ?? category.Names.GetValueOrDefault("en") ?? "Category",
                    PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(category.PictureUrl) ? defaultImage : category.PictureUrl)}",
                    SortOrder = category.SortOrder,
                    ChildCategories = category.ChildCategories
                        .OrderBy(sub => sub.SortOrder)
                        .Select(sub => new CategoryDisplayDto()
                        {
                            Id = sub.Id,
                            ParentCategoryId = sub.ParentCategoryId,
                            Name = sub.Names.GetValueOrDefault(languageCode) ?? sub.Names.GetValueOrDefault("en") ?? "Subcategory",
                            PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(sub.PictureUrl) ? defaultImage : sub.PictureUrl)}",
                            SortOrder = sub.SortOrder,
                            ChildCategories = new List<CategoryDisplayDto>()
                        }).ToList()
                };

                return Results.Ok(categoryDto);
            });

            //---------------------------------------------------------------Create category
            group.MapPost("/create", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db, IWebHostEnvironment env) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var form = await request.ReadFormAsync();
                var dtoStr = form["categoryData"];

                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Category data missing.");

                var dto = JsonSerializer.Deserialize<CategoryCreateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Unvalid category data.");

                string finalPhotoUrl = "default_category_picture.png";
                var file = form.Files.GetFile("image");

                if (file is { Length: > 0 })
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                        return Results.BadRequest("Unsupported file image.");

                    var uploadFolder = Path.Combine(env.WebRootPath, "images", "categories");
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
                                Size = new Size(800, 0)
                            }));

                            await image.SaveAsJpegAsync(filePath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                            {
                                Quality = 100
                            });
                        }
                        finalPhotoUrl = fileName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while processing image: {ex.Message}");
                    }
                }

                var newCategory = new Category
                {
                    Id = Guid.NewGuid(),
                    Names = dto.Names,
                    SortOrder = dto.SortOrder,
                    ParentCategoryId = dto.ParentCategory,
                    PictureUrl = finalPhotoUrl
                };

                db.Categories.Add(newCategory);
                await db.SaveChangesAsync();

                return Results.Ok(newCategory.Id);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Delete category
            group.MapDelete("/delete/{id:guid}", async (Guid id, PinulaDbContext db, IWebHostEnvironment env) =>
            {
                var category = await db.Categories
                    .Include(c => c.ChildCategories)
                    .Include(c => c.Recipes)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null) return Results.NotFound("Category not found.");

                if (category.ChildCategories.Any())
                    return Results.BadRequest("Category has subcategories");

                if (category.Recipes.Any())
                    return Results.BadRequest("Category is linked to recipes");

                if (category.PictureUrl != "default_category_picture.png")
                {
                    var filePath = Path.Combine(env.WebRootPath, "images", "categories", category.PictureUrl);
                    if (File.Exists(filePath)) File.Delete(filePath);
                }

                db.Categories.Remove(category);
                await db.SaveChangesAsync();

                return Results.NoContent();
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Get all categories admin (Flattened with Level)
            group.MapGet("/getAllAdmin", async (HttpRequest request, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/categories/";
                var defaultImage = "default_category.png";

                var allCategories = await db.Categories.AsNoTracking().ToListAsync();

                var rootCategories = allCategories.Where(c => c.ParentCategoryId == null || c.ParentCategoryId == Guid.Empty).OrderBy(c => c.SortOrder).ToList();

                var rootList = new List<AdminCategoryDisplayDto>();

                void RootCategoryTree(List<Category> currentLevelItems, int level)
                {
                    foreach (var cat in currentLevelItems)
                    {
                        var dto = new AdminCategoryDisplayDto()
                        {
                            Id = cat.Id,
                            ParentCategoryId = cat.ParentCategoryId,
                            Names = cat.Names, // Posíláme celý Dictionary se všemi překlady
                            PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(cat.PictureUrl) ? defaultImage : cat.PictureUrl)}",
                            SortOrder = cat.SortOrder,
                            Level = level
                        };
                        rootList.Add(dto);

                        var children = allCategories.Where(sub => sub.ParentCategoryId == cat.Id).OrderBy(sub => sub.SortOrder).ToList();

                        if (children.Any())
                        {
                            RootCategoryTree(children, level + 1);
                        }
                    }
                }

                RootCategoryTree(rootCategories, 0);
                return Results.Ok(rootList);
            }).RequireAuthorization("AdminOnly");



        }
    }
}
