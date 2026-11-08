using Pinula.Shared.DTOs;
using System.Security.Claims;
using System.Text.Json;
using System.Globalization;
using Pinula.API.Interface;

namespace Pinula.API.Endpoints
{
    public static class UserEndpoint
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/users").RequireRateLimiting("GeneralApiPolicy");
            
            //---------------------------------------------------------------Verify token
            group.MapGet("/verifyToken", async (ClaimsPrincipal user, IUserBackendService service) =>
            {
                var exists = await service.VerifyTokenAsync(user.GetUserId());
                return exists ? Results.Ok() : Results.NotFound();
            }).RequireAuthorization();
            
            //---------------------------------------------------------------Check email availability
            group.MapGet("/checkEmail", async (string email, IUserBackendService service) =>
            {
                var isAvailable = await service.CheckEmailAvailableAsync(email);
                return isAvailable ? Results.Ok() : Results.NotFound();
            });

            //---------------------------------------------------------------UserRegistration
            group.MapPost("/register", async (UserRegistrationDto dto, IUserBackendService service) =>
            {
                var result = await service.RegisterAsync(dto);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireRateLimiting("AuthStrictPolicy");

            //---------------------------------------------------------------UserLogin
            group.MapPost("/login", async (UserLoginDto dto, IUserBackendService service) =>
            {
                var result = await service.LoginAsync(dto);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireRateLimiting("AuthStrictPolicy");

            //---------------------------------------------------------------GetMe
            group.MapGet("/getMe", async (HttpRequest request, ClaimsPrincipal user, IUserBackendService service) =>
            {
                string baseUrl = $"{request.Scheme}://{request.Host}";
                var userData = await service.GetMeAsync(user.GetUserId(), baseUrl);
                return userData != null ? Results.Ok(userData) : Results.NotFound("User not found");
            }).RequireAuthorization();

            //---------------------------------------------------------------GetUserDisplay
            group.MapGet("/getUserDisplay/{userId:guid}", async (HttpRequest request, Guid userId, IUserBackendService service) =>
            {
                string baseUrl = $"{request.Scheme}://{request.Host}";
                var userData = await service.GetUserDisplayAsync(userId, baseUrl);
                return userData != null ? Results.Ok(userData) : Results.NotFound();
            });
            
            //---------------------------------------------------------------Update user
            group.MapPut("/update", async (HttpRequest request, ClaimsPrincipal user, IUserBackendService service) =>
            {
                var form = await request.ReadFormAsync();
                var dtoStr = form["userData"];
                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Missing user data.");

                var dto = JsonSerializer.Deserialize<UserUpdateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Invalid user data.");

                var file = form.Files.GetFile("image");
                var result = await service.UpdateUserAsync(user.GetUserId(), dto, file);
                return Results.Json(result, statusCode: result.StatusCode);
            }).DisableAntiforgery().RequireAuthorization();

            // --------------------------------------------------------------- Reset / Change password
            group.MapPost("/changePassword", async (PasswordChangeDto dto, IUserBackendService service) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                    return Results.BadRequest("Invalid email or password.");
                
                var result = await service.ChangePasswordAsync(dto);
                return Results.Json(result, statusCode: result.StatusCode);
            }).AllowAnonymous().RequireRateLimiting("AuthStrictPolicy");

            //---------------------------------------------------------------Get all users
            group.MapGet("/admin/all", async (IUserBackendService service) =>
            {
                var users = await service.AdminGetAllUsersAsync();
                return Results.Ok(users);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Admin change password
            group.MapPost("/admin/changePassword", async (AdminPasswordChangeDto dto, IUserBackendService service) =>
            {
                var result = await service.AdminChangePasswordAsync(dto);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Admin change comment permission
            group.MapPost("/admin/toggleCommentPermission/{userId:guid}", async (Guid userId, IUserBackendService service) =>
            {
                var result = await service.AdminToggleCommentPermissionAsync(userId);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Admin change recipe creation permission
            group.MapPost("/admin/toggleRecipePermission/{userId:guid}", async (Guid userId, IUserBackendService service) =>
            {
                var result = await service.AdminToggleRecipePermissionAsync(userId);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireAuthorization("AdminOnly");
            
            
            // --------------------------------------------------------------- Send verification code
            group.MapPost("/sendVerificationCode", async (VerificationCodeRequestDto dto, IUserBackendService service) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Email)) return Results.BadRequest("Email is required.");
                string lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                    
                var result = await service.SendVerificationCodeAsync(dto, lang);
                return Results.Json(result, statusCode: result.StatusCode);
            }).RequireRateLimiting("AuthStrictPolicy");

            // --------------------------------------------------------------- Verify code
            group.MapPost("/verifyCode", async (VerificationCodeVerifyDto dto, IUserBackendService service) =>
            {
                if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Code))
                    return Results.BadRequest("Email and code are required.");

                var result = await service.VerifyCodeAsync(dto);
                return Results.Json(result, statusCode: result.StatusCode);
            });
        }
    }
}