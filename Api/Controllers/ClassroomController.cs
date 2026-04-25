using Api.Contracts;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/classroom")]
public class ClassroomController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly GoogleClassroomClient _classroom;

    public ClassroomController(UserManager<ApplicationUser> userManager, GoogleClassroomClient classroom)
    {
        _userManager = userManager;
        _classroom = classroom;
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        try
        {
            var courses = await _classroom.GetTeacherCoursesAsync(user);
            var dto = courses
                .Select(c => new ClassroomCourseDto(c.Id, c.Name, c.Section, c.Description))
                .ToList();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { error = "Failed to fetch Google Classroom courses.", detail = ex.Message });
        }
    }
}
