using Pinula.Shared.DTOs;

namespace Pinula.API.Interface;

public interface IUnitBackendService
{
    Task<List<UnitPreviewDto>> GetServingUnitsAsync(string lang);
    Task<List<UnitPreviewDto>> GetUnitsAsync(string lang);
    Task<List<UnitDto>> GetAdminUnitsAsync();
    Task<GeneralResponse> CreateUnitAsync(UnitDto dto);
    Task<GeneralResponse> DeleteUnitAsync(Guid id);
}