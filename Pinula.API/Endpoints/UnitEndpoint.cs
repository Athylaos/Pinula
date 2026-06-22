using DeepL;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using System.Globalization;
using System.Security.Claims;

namespace Pinula.API.Endpoints
{
    public static class UnitEndpoint
    {
        public static void MapUnitEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/units");

            //---------------------------------------------------------------Get servingUnits
            group.MapGet("/getServing", async (PinulaDbContext db) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var units = await db.Units.AsNoTracking().Where(u => u.IsServingUnit).Select(u => new UnitPreviewDto { Id = u.Id, Name = u.Names.GetValueOrDefault(languageCode) ?? u.Names.GetValueOrDefault("en") ?? "UnitName" }).ToListAsync();

                return Results.Ok(units) ;
            });

            //---------------------------------------------------------------Get units
            group.MapGet("/get", async (PinulaDbContext db) =>
            {
                string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                var units = await db.Units.AsNoTracking().Select(u => new UnitPreviewDto { Id = u.Id, Name = u.Names.GetValueOrDefault(languageCode) ?? u.Names.GetValueOrDefault("en") ?? "UnitName" }).ToListAsync();

                return Results.Ok(units);
            });

            //---------------------------------------------------------------Create unit
            group.MapPost("/create", async (Unit unit, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                if (string.IsNullOrWhiteSpace(unit.Names["en"]) || string.IsNullOrWhiteSpace(unit.Names["cs"]))
                    return Results.BadRequest("Unit names is mandatory");

                if (string.IsNullOrWhiteSpace(unit.Code))
                    return Results.BadRequest("Code is mandatory");

                var newUnit = new Unit
                {
                    Id = Guid.NewGuid(),
                    Code = unit.Code,
                    Names = unit.Names,
                    IsServingUnit = unit.IsServingUnit
                };

                db.Units.Add(newUnit);
                await db.SaveChangesAsync();

                return Results.Ok(unit.Id);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Delete unit
            group.MapDelete("/{id:guid}", async (Guid id, PinulaDbContext db) =>
            {
                var unit = await db.Units.Include(u => u.IngredientUnits).FirstOrDefaultAsync(u => u.Id == id);

                if (unit == null) return Results.NotFound();

                if (unit.IngredientUnits.Any())
                {
                    return Results.BadRequest("The unit is used in ingredients, cant be deleted.");
                }

                db.Units.Remove(unit);
                await db.SaveChangesAsync();

                return Results.NoContent();
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Get units
            group.MapGet("/getAdmin", async (PinulaDbContext db) =>
            {
                var units = await db.Units.AsNoTracking().ToListAsync();

                return Results.Ok(units);
            }).RequireAuthorization("AdminOnly");

        }
    
    } 
}
