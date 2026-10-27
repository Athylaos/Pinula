using Mapster;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.API.Interface;
using Pinula.API.Models;
using Pinula.API.Projections;
using Pinula.Shared.DTOs;
using Pinula.Shared.Constants;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Pinula.API.Services;

public class CategoryBackendService : ICategoryBackendService
{
    private readonly PinulaDbContext _db;
    private readonly ILogger<CategoryBackendService> _logger;
    
    public CategoryBackendService(PinulaDbContext db, ILogger<CategoryBackendService> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<List<CategoryDisplayDto>> GetCategoriesTree(string baseUrl, string lang)
    {
        var allCategories = await _db.Categories
            .AsNoTracking()
            .Select(CategoryProjections.ToDisplayDto(baseUrl, lang))
            .ToListAsync();
        
        var categoriesByParent = allCategories
            .Where(c => c.ParentCategoryId != null)
            .ToLookup(c => c.ParentCategoryId!.Value);
        
        var rootCategories = allCategories
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.SortOrder)
            .ToList();

        List<CategoryDisplayDto> GetChildren(Guid? parentId)
        {
            if (parentId.HasValue && !categoriesByParent.Contains(parentId.Value))
                return new List<CategoryDisplayDto>();

            var currentLevel = parentId.HasValue
                ? categoriesByParent[parentId.Value].OrderBy(c => c.SortOrder).ToList()
                : rootCategories;

            var result = new List<CategoryDisplayDto>();
            foreach (var cat in currentLevel)
            {
                cat.ChildCategories = GetChildren(cat.Id);
                result.Add(cat);
            }
            return result;
        }

        return GetChildren(null);
    }

    public async Task<CategoryDisplayDto?> GetCategoryWithChildren(string baseUrl, string lang, Guid id)
    {
        var categoryDb = await _db.Categories.AsNoTracking().Include(c => c.ChildCategories).FirstOrDefaultAsync(c => c.Id == id);

        if (categoryDb is null) return null;

        var categoryDto = new CategoryDisplayDto()
        {
            Id = categoryDb.Id,
            ParentCategoryId = categoryDb.ParentCategoryId,
            Name = categoryDb.Names.GetValueOrDefault(lang) ?? categoryDb.Names.GetValueOrDefault("en") ?? "Category",
            PictureUrl = $"{baseUrl}{(string.IsNullOrWhiteSpace(categoryDb.PictureUrl) ? ImageConstants.DefaultCategory : categoryDb.PictureUrl)}",
            SortOrder = categoryDb.SortOrder,
            ChildCategories = categoryDb.ChildCategories
                .OrderBy(sub => sub.SortOrder)
                .Select(sub => new CategoryDisplayDto()
                {
                    Id = sub.Id,
                    ParentCategoryId = sub.ParentCategoryId,
                    Name = sub.Names.GetValueOrDefault(lang) ?? sub.Names.GetValueOrDefault("en") ?? "Subcategory",
                    PictureUrl = $"{baseUrl}{(string.IsNullOrWhiteSpace(sub.PictureUrl) ? ImageConstants.DefaultCategory : sub.PictureUrl)}",
                    SortOrder = sub.SortOrder,
                    ChildCategories = new List<CategoryDisplayDto>()
                }).ToList()
        };

        return categoryDto;
    }

    public async Task<CreateCategoryResponse> CreateCategory(CategoryCreateDto createDto, IFormFile? imageFile, IWebHostEnvironment webHostEnvironment)
    {
        var finalPhotoUrl = ImageConstants.DefaultCategory;
        
        if (imageFile is { Length: > 0 })
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return new CreateCategoryResponse()
                    { StatusCode = 400, Message = "Unsupported file format"};

            var uploadFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "categories");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid()}.webp";
            var filePath = Path.Combine(uploadFolder, fileName);

            try
            {
                using (var image = await Image.LoadAsync(imageFile.OpenReadStream()))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(800, 0)
                    }));

                    await image.SaveAsWebpAsync(filePath, new SixLabors.ImageSharp.Formats.Webp.WebpEncoder()
                    {
                        Quality = 100
                    });
                }
                finalPhotoUrl = fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing image for category creation");
                return new CreateCategoryResponse()
                    { StatusCode = 500, Message = "Error while processing image"};
            }
        }
        
        var newCategory = new Category
        {
            Id = Guid.NewGuid(),
            Names = createDto.Names,
            SortOrder = createDto.SortOrder,
            ParentCategoryId = createDto.ParentCategory,
            PictureUrl = finalPhotoUrl
        };
        
        _db.Categories.Add(newCategory);
        await _db.SaveChangesAsync();
        
        return new CreateCategoryResponse()
            { StatusCode = 201, Message = "Category created", CreatedCategoryId = newCategory.Id};
    }

    public async Task<GeneralResponse> DeleteCategory(Guid id, IWebHostEnvironment webHostEnvironment)
    {
        var category = await _db.Categories
            .Include(c => c.ChildCategories)
            .Include(c => c.Recipes)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null) return new() { StatusCode = 404, Message = "Category not found", Successful = false };
        if (category.ChildCategories.Any()) return new() { StatusCode = 400, Message = "Category has subcategories", Successful = false };
        if (category.Recipes.Any()) return new() { StatusCode = 400, Message = "Category is linked to recipes", Successful = false };
        
        if (category.PictureUrl != ImageConstants.DefaultCategory)
        {
            var filePath = Path.Combine(webHostEnvironment.WebRootPath, "images", "categories", category.PictureUrl);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return new() { StatusCode = 204, Message = "Category deleted", Successful = true };
    }
    
    public async Task<List<AdminCategoryDisplayDto>> GetAllAdminCategoriesFlattened(string baseUrl)
    {
        var allCategories = await _db.Categories
            .AsNoTracking()
            .ToListAsync();

        var categoriesByParent = allCategories
            .Where(c => c.ParentCategoryId != null && c.ParentCategoryId != Guid.Empty)
            .ToLookup(c => c.ParentCategoryId!.Value);

        var rootCategories = allCategories
            .Where(c => c.ParentCategoryId == null || c.ParentCategoryId == Guid.Empty)
            .OrderBy(c => c.SortOrder);

        var result = new List<AdminCategoryDisplayDto>();

        void Traverse(IEnumerable<Category> currentLevelItems, int level)
        {
            foreach (var cat in currentLevelItems)
            {
                result.Add(new AdminCategoryDisplayDto
                {
                    Id = cat.Id,
                    ParentCategoryId = cat.ParentCategoryId,
                    Names = cat.Names,
                    PictureUrl = $"{baseUrl}{(string.IsNullOrWhiteSpace(cat.PictureUrl) ? ImageConstants.DefaultCategory : cat.PictureUrl)}",
                    SortOrder = cat.SortOrder,
                    Level = level
                });

                if (categoriesByParent.Contains(cat.Id))
                {
                    Traverse(categoriesByParent[cat.Id].OrderBy(c => c.SortOrder), level + 1);
                }
            }
        }

        Traverse(rootCategories, 0);
        return result;
    }
}