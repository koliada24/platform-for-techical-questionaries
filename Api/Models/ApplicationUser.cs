using Microsoft.AspNetCore.Identity;

namespace Api.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public UserRole Role { get; set; } = UserRole.Student;

    // Google integration fields
    public string? GoogleId { get; set; }
    public string? PictureUrl { get; set; }
    public string? GoogleAccessToken { get; set; }
    public string? GoogleRefreshToken { get; set; }
    public DateTimeOffset? GoogleTokenExpiresAt { get; set; }
}

public enum UserRole
{
    Student = 0,
    Teacher = 1
}
