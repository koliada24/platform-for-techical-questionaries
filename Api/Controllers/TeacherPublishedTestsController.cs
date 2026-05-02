using System.Security.Claims;
using Api.Services.Published;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/teacher/published-tests")]
public class TeacherPublishedTestsController : ControllerBase
{
    private readonly IPublishedTestsService _service;

    public TeacherPublishedTestsController(IPublishedTestsService service)
    {
        _service = service;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Missing user id claim.");

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await _service.ListForTeacherAsync(CurrentUserId, ct);
        return Ok(items);
    }

    [HttpGet("details")]
    public async Task<IActionResult> Details(
        [FromQuery] Guid testTemplateId,
        [FromQuery] DateTimeOffset closesAt,
        CancellationToken ct)
    {
        var detail = await _service.GetDetailForTeacherAsync(CurrentUserId, testTemplateId, closesAt, ct);
        return detail is null ? NotFound() : Ok(detail);
    }
}
