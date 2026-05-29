using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using System.Security.Claims;

namespace Pinula.API.Endpoints
{
    public static class MealPlanEndpoint
    {
        public static void MapMealPlanEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/mealplan");



            //---------------------------------------------------------------Get meal plans
            group.MapGet("/get", async (HttpRequest request,DateTime fromDate, DateTime toDate, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/recipes/";
                var defaultImage = "default_recipe.png";

                var userImageBaseUrl = $"{request.Scheme}://{request.Host}/images/avatars/";
                var userDefaultImage = "default_avatar.png";

                var userId = user.GetUserId();
                var userDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                DateTime utcFromDate = DateTime.SpecifyKind(fromDate, DateTimeKind.Utc);
                DateTime utcToDate = DateTime.SpecifyKind(toDate, DateTimeKind.Utc);

                var mealplans = await db.MealPlans
                    .AsNoTracking()
                    .Where(mp => mp.GroupId == groupId && mp.Date.Date.ToUniversalTime() >= utcFromDate.Date && mp.Date.Date.ToUniversalTime() <= utcToDate.Date)
                    .OrderBy(mp => mp.Date)
                    .ThenBy(mp => mp.MealType)
                    .Select(mp => new MealPlanPreviewDto
                    {
                        Id = mp.Id,
                        Date = mp.Date,
                        MealType = mp.MealType,
                        Servings = mp.Servings,
                        RecipeId = mp.RecipeId,
                        RecipeName = mp.Recipe.Title,
                        RecipePhotoUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(mp.Recipe.PhotoUrl) ? defaultImage : mp.Recipe.PhotoUrl)}",
                        UsersPreviews = mp.Users.Select(u => new UserDisplayDto
                        {
                            Id = u.Id,
                            Name = u.Name,
                            Surname = u.Surname,
                            AvatarUrl = $"{userImageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? userDefaultImage : u.AvatarUrl)}",
                        }).ToList()
                    }).ToListAsync();

                return Results.Ok(mealplans);

            }).RequireAuthorization();

            //---------------------------------------------------------------Create meal plan
            group.MapPost("/add", async (CreateMealPlanDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var users = await db.Users.Where(u => dto.UsersId.Contains(u.Id) && u.GroupId == groupId).ToListAsync();

                if (!users.Any()) return Results.BadRequest("At least one user from the group has to be selected.");

                var mealPlan = new MealPlan
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.SpecifyKind(dto.Date.Date, DateTimeKind.Utc),
                    MealType = dto.MealType,
                    RecipeId = dto.RecipeId,
                    GroupId = groupId.Value,
                    Servings = dto.Servings,
                    Users = users
                };

                db.MealPlans.Add(mealPlan);
                await db.SaveChangesAsync();

                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Delete meal plan
            group.MapDelete("/delete/{id:guid}", async (Guid id, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var mealPlan = await db.MealPlans.FirstOrDefaultAsync(mp => mp.Id == id);
                if (mealPlan is null) return Results.NotFound();

                if (mealPlan.GroupId != groupId) return Results.Unauthorized();

                db.MealPlans.Remove(mealPlan);
                await db.SaveChangesAsync();

                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Update meal plan
            group.MapPut("/update/{id:guid}", async (Guid id, UpdateMealPlanDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var mealPlan = await db.MealPlans.Include(mp => mp.Users).FirstOrDefaultAsync(mp => mp.Id == id);
                if (mealPlan is null) return Results.NotFound();
                if (mealPlan.GroupId != groupId) return Results.Unauthorized();

                var newUsers = await db.Users.Where(u => dto.UsersIds.Contains(u.Id) && u.GroupId == groupId).ToListAsync();
                if (!newUsers.Any()) return Results.BadRequest("At least one user must be selected.");

                mealPlan.Date = dto.Date.Date;
                mealPlan.MealType = dto.MealType;
                mealPlan.Servings = dto.Servings;
                mealPlan.Users = newUsers;

                await db.SaveChangesAsync();
                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Create group
            group.MapPost("/group/create", async (CreateGroupDto dto, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");
         
                if (userDb.GroupId is not null) return Results.BadRequest("User is already in group");

                var rawCode = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 12).ToUpper();
                var inviteCode = $"{rawCode.Substring(0, 4)}-{rawCode.Substring(4, 4)}-{rawCode.Substring(8, 4)}";

                var newGroup = new Group
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    InviteCode = inviteCode
                };

                db.Groups.Add(newGroup);
                userDb.GroupId = newGroup.Id;
                await db.SaveChangesAsync();

                return Results.Ok(new GroupDetailDto
                {
                    Id = newGroup.Id,
                    Name = newGroup.Name,
                    InviteCode = newGroup.InviteCode
                });

            }).RequireAuthorization();

            //---------------------------------------------------------------Join group
            group.MapPost("/group/join/{code}", async (string code, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                if (userDb.GroupId is not null) return Results.BadRequest("User is already in group");

                var group = await db.Groups.FirstOrDefaultAsync(g => g.InviteCode.Replace("-", "").ToLower() == code.Replace("-", "").Trim().ToLower());
                if (group is null) return Results.NotFound("Invalid invitation code");

                userDb.GroupId = group.Id;
                await db.SaveChangesAsync();

                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Get my group
            group.MapGet("/group/my", async (ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var group = await db.Groups
                    .AsNoTracking()
                    .Where(g => g.Id == userDb.GroupId)
                    .Select(g => new GroupDetailDto
                    {
                        Id = g.Id,
                        Name = g.Name,
                        InviteCode = g.InviteCode
                    })
                    .FirstOrDefaultAsync();

                return Results.Ok(group);

            }).RequireAuthorization();

            //---------------------------------------------------------------Leave my group
            group.MapPost("/group/leave", async (ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                userDb.GroupId = null;

                if (!await db.Users.AnyAsync(u => u.GroupId == groupId && u.Id != userId))
                {
                    var groupToDelete = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId);
                    if (groupToDelete is not null)
                    {
                        db.Groups.Remove(groupToDelete);
                    }
                }

                await db.SaveChangesAsync();

                return Results.Ok();

            }).RequireAuthorization();

            //---------------------------------------------------------------Get members
            group.MapGet("/group/members", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var userImageBaseUrl = $"{request.Scheme}://{request.Host}/images/avatars/";
                var userDefaultImage = "default_avatar.png";

                var userId = user.GetUserId();
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (userDb is null) return Results.BadRequest("User not found");

                var groupId = userDb.GroupId;
                if (groupId is null) return Results.BadRequest("User is not in group");

                var members = await db.Users
                    .AsNoTracking()
                    .Where(u => u.GroupId == groupId)
                    .Select(u => new UserDisplayDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Surname = u.Surname,
                        AvatarUrl = $"{userImageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? userDefaultImage : u.AvatarUrl)}",
                    }).ToListAsync();

                return Results.Ok(members);

            }).RequireAuthorization();


        }
    }
}
