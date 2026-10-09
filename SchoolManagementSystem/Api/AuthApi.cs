using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using SchoolManagementSystem.Data;
using SchoolManagementSystem.Services;

namespace SchoolManagementSystem.Api;

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresUtc,
    string UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);

public record CurrentUserResponse(string UserId, string Email, string FullName, IReadOnlyList<string> Roles);

public static class AuthApi
{
    /// <summary>
    /// JSON auth endpoints for clients other than the Blazor pages (mobile apps, scripts, other sites).
    ///   POST /api/auth/login  -> { accessToken, tokenType, expiresUtc, ... }
    ///   GET  /api/auth/me     -> current user; send "Authorization: Bearer &lt;token&gt;"
    /// </summary>
    public static IEndpointRouteBuilder MapAuthApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
                LoginRequest request,
                UserManager<ApplicationUser> users,
                SignInManager<ApplicationUser> signIn,
                JwtTokenService tokens) =>
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                    return Results.BadRequest(new { error = "Email and password are required." });

                var user = await users.FindByEmailAsync(request.Email.Trim());
                // Same response for unknown user, wrong password, and inactive account: don't reveal which.
                if (user is null || !user.IsActive)
                    return Results.Unauthorized();

                var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
                if (result.IsLockedOut)
                    return Results.Json(new { error = "Account is locked after too many failed attempts. Try again later." }, statusCode: StatusCodes.Status423Locked);
                if (!result.Succeeded)
                    return Results.Unauthorized();

                var roles = await users.GetRolesAsync(user);
                var (token, expires) = tokens.Create(user, roles);

                return Results.Ok(new LoginResponse(
                    token, "Bearer", expires, user.Id, user.Email ?? string.Empty, user.FullName ?? string.Empty, roles.ToList()));
            })
            .AllowAnonymous()
            .WithName("Login");

        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<ApplicationUser> users) =>
            {
                var user = await users.GetUserAsync(principal);
                if (user is null || !user.IsActive) return Results.Unauthorized();

                var roles = await users.GetRolesAsync(user);
                return Results.Ok(new CurrentUserResponse(user.Id, user.Email ?? string.Empty, user.FullName ?? string.Empty, roles.ToList()));
            })
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .WithName("CurrentUser");

        return app;
    }
}
