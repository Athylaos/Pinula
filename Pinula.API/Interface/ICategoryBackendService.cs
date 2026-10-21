using Pinula.Shared.DTOs;

namespace Pinula.API.Interface;

public interface ICategoryBackendService
{
    public Task<List<CategoryDisplayDto>> GetCategoriesTree(string baseUrl, string lang);
}