using System.Linq.Expressions;
using Pinula.API.Models;
using Pinula.Shared.DTOs;
using Pinula.Shared.Constants;

namespace Pinula.API.Projections;

public static class CategoryProjections
{
    public static Expression<Func<Category, CategoryDisplayDto>> ToDisplayDto(string baseUrl, string lang)
    {

        return c => new CategoryDisplayDto
        {
            Id = c.Id,
            ParentCategoryId = c.ParentCategoryId,
            Name = c.Names.ContainsKey(lang) 
                ? (string.IsNullOrWhiteSpace(c.Names[lang]) ? (c.Names.ContainsKey("en") ? c.Names["en"] : "Category") : c.Names[lang])
                : (c.Names.ContainsKey("en") ? c.Names["en"] : "Category"),
            PictureUrl = baseUrl + (string.IsNullOrWhiteSpace(c.PictureUrl) ? ImageConstants.DefaultCategory : c.PictureUrl),
            SortOrder = c.SortOrder
        };
    }
    
}