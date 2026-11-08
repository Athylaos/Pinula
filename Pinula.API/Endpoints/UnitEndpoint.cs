using Pinula.Shared.DTOs;
using System.Globalization;
using Pinula.API.Interface;

namespace Pinula.API.Endpoints
{
    public static class UnitEndpoint
    {
        public static void MapUnitEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/units").RequireRateLimiting("GeneralApiPolicy");

            //---------------------------------------------------------------Get servingUnits
            group.MapGet("/getServing", async (IUnitBackendService service) =>
            {
                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var units = await service.GetServingUnitsAsync(lang);
                return Results.Ok(units);
            });

            //---------------------------------------------------------------Get units
            group.MapGet("/get", async (IUnitBackendService service) =>
            {
                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var units = await service.GetUnitsAsync(lang);
                return Results.Ok(units);
            });

            //---------------------------------------------------------------Create unit
            group.MapPost("/create", async (UnitDto unit, IUnitBackendService service) =>
            {
                var result = await service.CreateUnitAsync(unit);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Delete unit
            group.MapDelete("/{id:guid}", async (Guid id, IUnitBackendService service) =>
            {
                var result = await service.DeleteUnitAsync(id);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Get units admin
            group.MapGet("/getAdmin", async (IUnitBackendService service) =>
            {
                var units = await service.GetAdminUnitsAsync();
                return Results.Ok(units);
            }).RequireAuthorization("AdminOnly");

        }
    
    } 
}
