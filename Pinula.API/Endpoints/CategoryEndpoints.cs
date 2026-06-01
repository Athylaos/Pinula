using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
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

                return await db.Categories.AsNoTracking().Include(c => c.ChildCategories).OrderBy(c => c.SortOrder).Select(c => new Category() { 
                    ChildCategories = c.ChildCategories,
                    ParentCategoryId = c.ParentCategoryId,
                    Id = c.Id,
                    Name = c.Name,
                    PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(c.PictureUrl) ? defaultImage : c.PictureUrl)}",
                    SortOrder = c.SortOrder
                }).ToListAsync();
            });


            //---------------------------------------------------------------Get main categories
            group.MapGet("/getMain", async (HttpRequest request, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/categories/";
                var defaultImage = "default_category.png";

                return await db.Categories.AsNoTracking().Where(c => c.ParentCategoryId == null).Include(c => c.ChildCategories).OrderBy(c => c.SortOrder).Select(c => new Category()
                {
                    ChildCategories = c.ChildCategories,
                    ParentCategoryId = c.ParentCategoryId,
                    Id = c.Id,
                    Name = c.Name,
                    PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(c.PictureUrl) ? defaultImage : c.PictureUrl)}",
                    SortOrder = c.SortOrder
                }).ToListAsync();
            });

            //---------------------------------------------------------------Get category
            group.MapGet("/get/{categoryId:guid}", async (HttpRequest request, Guid categoryId, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/categories/";
                var defaultImage = "default_category.png";

                var category = await db.Categories.AsNoTracking().Include(c => c.ChildCategories).FirstOrDefaultAsync(c => c.Id == categoryId);

                if(category is null)
                {
                    return Results.BadRequest(category);
                }
                else
                {
                    category.PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(category.PictureUrl) ? defaultImage : category.PictureUrl)}";
                    foreach(var sub in category.ChildCategories)
                    {
                        sub.PictureUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(sub.PictureUrl) ? defaultImage : sub.PictureUrl)}";
                    }
                    return Results.Ok(category);
                }
            });

            //---------------------------------------------------------------Create category
            group.MapPost("/create", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db, IWebHostEnvironment env) =>
            {
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
                    Name = dto.Name,
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



        }
    }
}
