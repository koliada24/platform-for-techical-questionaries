using Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Api.Services;

public interface ITeacherProvider
{
    Task<ApplicationUser?> GetTeacherAsync(string teacherId, CancellationToken ct = default);
}

public class TeacherProvider : ITeacherProvider
{
    private readonly UserManager<ApplicationUser> _userManager;

    public TeacherProvider(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ApplicationUser?> GetTeacherAsync(string teacherId, CancellationToken ct = default)
    {
        return await _userManager.FindByIdAsync(teacherId);
    }
}
