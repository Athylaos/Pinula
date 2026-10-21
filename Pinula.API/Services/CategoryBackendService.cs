using Mapster;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.API.Interface;
using Pinula.API.Models;
using Pinula.API.Projections;
using Pinula.Shared.DTOs;

namespace Pinula.API.Services;

public class CategoryBackendService : ICategoryBackendService
{
    private readonly PinulaDbContext _db;
    public CategoryBackendService(PinulaDbContext db)
    {
        _db = db;
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
}