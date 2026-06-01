using Pinula.Shared.DTOs;
using Pinula.Shared.Models;

namespace Pinula.Shared.Interface
{
    public interface IUnitService
    {
        public Task<List<UnitPreviewDto>> GetAllUnitsAsync();
        public Task<List<UnitPreviewDto>> GetAllServingUnitsAsync();
        public Task<List<UnitPreviewDto>> GetIngredientUnitsAsync(Guid ingredientId);

        public Task<bool> CreateUnitAsync(Unit unit);
        public Task<bool> DeleteUnitAsync(Guid unitId);


    }
}
