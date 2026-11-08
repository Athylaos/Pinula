using Pinula.Shared.DTOs;

namespace Pinula.Shared.Interface
{
    public interface IUnitApiClient
    {
        public Task<List<UnitPreviewDto>> GetAllServingUnitsAsync();
        public Task<List<UnitPreviewDto>> GetAllUnitsAsync();
        public Task<List<UnitDto>> GetAllUnitsAdminAsync();
        
        public Task<GeneralResponse> CreateUnitAsync(UnitDto unit);
         public Task<GeneralResponse> DeleteUnitAsync(Guid unitId);
    }
}
