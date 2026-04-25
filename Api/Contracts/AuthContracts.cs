using Api.Models;

namespace Api.Contracts;

public record UserDto(
    string Email,
    string? FullName,
    UserRole Role,
    bool HasGoogleLink
);
