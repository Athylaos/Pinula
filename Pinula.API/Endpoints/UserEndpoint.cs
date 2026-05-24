using BCrypt.Net;
using Pinula.API.Context;
using Pinula.Shared.DTOs;
using Pinula.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.IdentityModel.Tokens;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Pinula.API.Endpoints
{
    public static class UserEndpoint
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/users");

            //---------------------------------------------------------------UserRegistration
            group.MapPost("/register", async (UserRegistrationDto registrationDto, PinulaDbContext db) =>
            {
                if (await db.Users.AnyAsync(u => u.Email == registrationDto.Email))
                {
                    return Results.BadRequest("Email already in use");
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(registrationDto.Password);

                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = registrationDto.Email,
                    PasswordHash = passwordHash,
                    Name = registrationDto.Name,
                    Surname = registrationDto.Surname,
                    UserCreated = DateTime.UtcNow,
                    Role = "user"
                };

                db.Users.Add(newUser);
                await db.SaveChangesAsync();

                return Results.Ok(new { newUser.Id, newUser.Email });
            });

            //---------------------------------------------------------------UserLogin
            group.MapPost("/login", async (UserLoginDto loginDto, PinulaDbContext db, IConfiguration config) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return Results.Unauthorized();
                }

                var token = GenerateJwtToken(user, config);

                return Results.Ok(new LoginResponse
                {
                    Token = token,
                    User = new User()
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Name = user.Name,
                        Surname = user.Surname,
                        UserCreated = user.UserCreated,
                        Role = user.Role,
                        AvatarUrl = user.AvatarUrl,
                        PasswordHash = string.Empty
                    }
                });
            });


            //---------------------------------------------------------------GetMe
            group.MapGet("/getMe", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/avatars/";
                var defaultImage = "default_avatar.png";

                var userId = user.GetUserId();

                UserDisplayDto? userData = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new UserDisplayDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Surname = u.Surname,
                    Email = u.Email,
                    AvatarUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? defaultImage : u.AvatarUrl)}",
                    UserCreated = u.UserCreated,
                    CanComment = u.CanComment,
                    CanCreateRecipes = u.CanCreateRecipes
                }).FirstOrDefaultAsync();

                if (userData == null) return Results.NotFound("User not found");

                userData.PostedRecipes = await db.Recipes.AsNoTracking().Where(r => r.UserId == userId).CountAsync();
                userData.PostedComments = await db.Comments.AsNoTracking().Where(c => c.UserId == userId).CountAsync();
                var ratingsQuery = db.Recipes.AsNoTracking().Where(r => r.UserId == userId);

                if (await ratingsQuery.AnyAsync())
                {
                    userData.AvgRating = await ratingsQuery.AverageAsync(r => r.Rating)??0;
                }
                else
                {
                    userData.AvgRating = 0;
                }


                return Results.Ok(userData);
            }).RequireAuthorization();


            //---------------------------------------------------------------GetUserDisplay
            group.MapGet("/getUserDisplay/{userId:guid}", async (HttpRequest request, Guid userId,PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/avatars/";
                var defaultImage = "default_avatar.png";

                var userData = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => new UserDisplayDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Surname = u.Surname,
                    AvatarUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(u.AvatarUrl) ? defaultImage : u.AvatarUrl)}",
                    UserCreated = u.UserCreated,
                    CanComment = u.CanComment,
                    CanCreateRecipes = u.CanCreateRecipes,
                }).FirstOrDefaultAsync();

                return Results.Ok(userData);
            });


            //---------------------------------------------------------------Upadate user
            group.MapPut("/update", async (HttpRequest request, ClaimsPrincipal user, PinulaDbContext db, IWebHostEnvironment env) =>
            {
                var form = await request.ReadFormAsync();

                var dtoStr = form["userData"];
                if (string.IsNullOrEmpty(dtoStr)) return Results.BadRequest("Missing user data.");

                var dto = JsonSerializer.Deserialize<UserUpdateDto>(dtoStr!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto == null) return Results.BadRequest("Invalid user data.");

                var uId = user.GetUserId();
                var dbUser = await db.Users.FindAsync(uId);
                if (dbUser == null) return Results.NotFound("User not found");

                var file = form.Files.GetFile("image");
                if (file is { Length: > 0 })
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                        return Results.BadRequest("Unsupported image format.");

                    var uploadFolder = Path.Combine(env.WebRootPath, "images", "avatars");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    try
                    {
                        using (var image = await Image.LoadAsync(file.OpenReadStream()))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions
                            {
                                Mode = ResizeMode.Crop,
                                Size = new Size(600, 600)
                            }));

                            await image.SaveAsJpegAsync(filePath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 80 });
                        }
                        if (!string.IsNullOrEmpty(dbUser.AvatarUrl) && dbUser.AvatarUrl != "default_avatar.png")
                        {
                            var oldPath = Path.Combine(uploadFolder, dbUser.AvatarUrl);
                            if (File.Exists(oldPath)) File.Delete(oldPath);
                        }

                        dbUser.AvatarUrl = fileName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Image Processing Error: {ex.Message}");
                    }
                }
                if (!string.IsNullOrWhiteSpace(dto.Name)) dbUser.Name = dto.Name.Trim();
                if (!string.IsNullOrWhiteSpace(dto.Surname)) dbUser.Surname = dto.Surname.Trim();

                await db.SaveChangesAsync();
                return Results.Ok();
            }).DisableAntiforgery();


            //---------------------------------------------------------------Get all users
            group.MapGet("/admin/all", async (HttpRequest request, PinulaDbContext db) =>
            {
                var imageBaseUrl = $"{request.Scheme}://{request.Host}/images/avatars/";
                var defaultImage = "default_avatar.png";

                var users = await db.Users.ToListAsync();

                foreach(var user in users)
                {
                    user.AvatarUrl = $"{imageBaseUrl}{(string.IsNullOrWhiteSpace(user.AvatarUrl) ? defaultImage : user.AvatarUrl)}";
                }

                return users;

            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Admin change password
            group.MapPost("/admin/changePassword", async (ClaimsPrincipal user, AdminPasswordChangeDto dto, PinulaDbContext db) =>
            {
                var userDb = await db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
                if (userDb is null) return Results.NotFound();

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

                userDb.PasswordHash = passwordHash;
                db.SaveChanges();

                return Results.Ok();


            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Admin change comment permission
            group.MapPost("/admin/toggleCommentPermission/{userId:guid}", async (Guid userId, PinulaDbContext db) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user is null) return Results.NotFound("User not found.");

                user.CanComment = !user.CanComment;

                await db.SaveChangesAsync();
                return Results.Ok(new { canComment = user.CanComment });
            }).RequireAuthorization("AdminOnly");

            //---------------------------------------------------------------Admin change recipe creation permission
            group.MapPost("/admin/toggleRecipePermission/{userId:guid}", async (Guid userId, PinulaDbContext db) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user is null) return Results.NotFound("User not found.");

                user.CanCreateRecipes = !user.CanCreateRecipes;

                await db.SaveChangesAsync();
                return Results.Ok(new { canCreateRecipes = user.CanCreateRecipes });
            }).RequireAuthorization("AdminOnly");

        }

        
        private static string GenerateJwtToken(User user, IConfiguration config)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyString = config["JWTKey:Default"];

            if (string.IsNullOrEmpty(keyString))
                throw new Exception("JWT Key is missing in appsettings.json");

            var key = Encoding.ASCII.GetBytes(keyString);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role ?? "user")
                }),
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}