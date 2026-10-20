using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface
{
    public interface IUnitApiClient
    {
        public Task<List<UnitPreviewDto>> GetAllUnitsAsync();
        public Task<List<UnitPreviewDto>> GetAllServingUnitsAsync();

        public Task<bool> CreateUnitAsync(UnitDto unit);
        public Task<bool> DeleteUnitAsync(Guid unitId);

        public Task<List<UnitDto>> GetAllUnitsAdminAsync();
    }
}
