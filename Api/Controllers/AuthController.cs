using System.Security.Claims;
using Api.Contracts;
using Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        return Ok(ToDto(user));
    }

    // ----- Google OAuth: Teacher -----
    // The Google handler intercepts /api/auth/google-callback-teacher (its CallbackPath)
    // and, on success, redirects to this action at a *different* path to finalize sign-in.
    [HttpGet("google-login-teacher")]
    public IActionResult GoogleLoginTeacher([FromQuery] string? returnUrl = null)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCompleteTeacher), new { returnUrl })
        };
        return Challenge(props, "Google-Teacher");
    }

    [HttpGet("google-complete-teacher")]
    public Task<IActionResult> GoogleCompleteTeacher([FromQuery] string? returnUrl = null)
        => HandleGoogleCallback("External-Teacher", UserRole.Teacher, returnUrl);

    // ----- Google OAuth: Student -----
    [HttpGet("google-login-student")]
    public IActionResult GoogleLoginStudent([FromQuery] string? returnUrl = null)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCompleteStudent), new { returnUrl })
        };
        return Challenge(props, "Google-Student");
    }

    [HttpGet("google-complete-student")]
    public Task<IActionResult> GoogleCompleteStudent([FromQuery] string? returnUrl = null)
        => HandleGoogleCallback("External-Student", UserRole.Student, returnUrl);

    private async Task<IActionResult> HandleGoogleCallback(string externalScheme, UserRole role, string? returnUrl)
    {
        var result = await HttpContext.AuthenticateAsync(externalScheme);
        if (!result.Succeeded || result.Principal is null)
            return Redirect(BuildClientUrl("/login?error=google"));

        var googleId = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = result.Principal.FindFirstValue(ClaimTypes.Email);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            return Redirect(BuildClientUrl("/login?error=google"));

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = name,
                Role = role,
                GoogleId = googleId
            };
            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded)
                return Redirect(BuildClientUrl("/login?error=google"));
            await _userManager.AddToRoleAsync(user, role.ToString());
        }
        else
        {
            user.GoogleId = googleId;
            // Lock role to original; if mismatched, redirect with error
            if (user.Role != role)
            {
                await HttpContext.SignOutAsync(externalScheme);
                return Redirect(BuildClientUrl($"/login?error=role-mismatch&expected={user.Role}"));
            }
        }

        // Persist Google tokens for later Classroom API calls
        user.GoogleAccessToken = result.Properties?.GetTokenValue("access_token");
        user.GoogleRefreshToken = result.Properties?.GetTokenValue("refresh_token") ?? user.GoogleRefreshToken;
        var expires = result.Properties?.GetTokenValue("expires_at");
        if (DateTimeOffset.TryParse(expires, out var exp))
            user.GoogleTokenExpiresAt = exp;

        await _userManager.UpdateAsync(user);

        // Sign out the external scheme & sign in the app cookie
        await HttpContext.SignOutAsync(externalScheme);
        await SignInUserAsync(user);

        var target = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
        return Redirect(BuildClientUrl(target));
    }

    private async Task SignInUserAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email!),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
    }

    private string BuildClientUrl(string path)
    {
        var baseUrl = _configuration["Client:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
        if (!path.StartsWith('/')) path = "/" + path;
        return baseUrl + path;
    }

    private static UserDto ToDto(ApplicationUser u) =>
        new(u.Email!, u.FullName, u.Role, !string.IsNullOrEmpty(u.GoogleId));
}
