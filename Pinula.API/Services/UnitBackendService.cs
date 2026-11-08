using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;
using Pinula.API.Interface;
using Pinula.API.Models;
using Pinula.Shared.DTOs;

namespace Pinula.API.Services;

public class UnitBackendService : IUnitBackendService
{
    private readonly PinulaDbContext _db;
    private readonly ILogger<UnitBackendService> _logger;

    public UnitBackendService(PinulaDbContext db, ILogger<UnitBackendService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<UnitPreviewDto>> GetServingUnitsAsync(string lang)
    {
        return await _db.Units.AsNoTracking()
            .Where(u => u.IsServingUnit)
            .Select(u => new UnitPreviewDto
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Names.GetValueOrDefault(lang) ?? u.Names.GetValueOrDefault("en") ?? "UnitName"
            })
            .ToListAsync();
    }

    public async Task<List<UnitPreviewDto>> GetUnitsAsync(string lang)
    {
        return await _db.Units.AsNoTracking()
            .Select(u => new UnitPreviewDto
            {
                Id = u.Id,
                Code = u.Code,
                Name = u.Names.GetValueOrDefault(lang) ?? u.Names.GetValueOrDefault("en") ?? "UnitName"
            })
            .ToListAsync();
    }

    public async Task<List<UnitDto>> GetAdminUnitsAsync()
    {
        return await _db.Units.AsNoTracking()
            .Select(u => new UnitDto
            {
                Id = u.Id,
                Code = u.Code,
                Names = u.Names,
                IsServingUnit = u.IsServingUnit
            })
            .ToListAsync();
    }

    public async Task<GeneralResponse> CreateUnitAsync(UnitDto dto)
    {
        if (dto.Names == null || string.IsNullOrWhiteSpace(dto.Names.GetValueOrDefault("en")) || string.IsNullOrWhiteSpace(dto.Names.GetValueOrDefault("cs")))
        {
            return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Unit names are mandatory." };
        }

        if (string.IsNullOrWhiteSpace(dto.Code))
        {
            return new GeneralResponse { Successful = false, StatusCode = 400, Message = "Code is mandatory." };
        }

        try
        {
            var newUnit = new Unit
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Names = dto.Names,
                IsServingUnit = dto.IsServingUnit
            };

            _db.Units.Add(newUnit);
            await _db.SaveChangesAsync();

            return new GeneralResponse { Successful = true, StatusCode = 201, Id = newUnit.Id, Message = "Unit created successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating unit with code {Code}", dto.Code);
            return new GeneralResponse { Successful = false, StatusCode = 500, Message = "An error occurred while creating the unit." };
        }
    }

    public async Task<GeneralResponse> DeleteUnitAsync(Guid id)
    {
        try
        {
            var unit = await _db.Units.Include(u => u.IngredientUnits).FirstOrDefaultAsync(u => u.Id == id);

            if (unit == null)
            {
                return new GeneralResponse { Successful = false, StatusCode = 404, Message = "Unit not found." };
            }

            if (unit.IngredientUnits.Any())
            {
                return new GeneralResponse { Successful = false, StatusCode = 400, Message = "The unit is used in ingredients and cannot be deleted." };
            }

            _db.Units.Remove(unit);
            await _db.SaveChangesAsync();

            return new GeneralResponse { Successful = true, StatusCode = 200, Message = "Unit deleted successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting unit {UnitId}", id);
            return new GeneralResponse { Successful = false, StatusCode = 500, Message = "An error occurred while deleting the unit." };
        }
    }
}