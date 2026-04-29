using System.Security.Claims;
using Api.Services.InProgress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api")]
public class TestsInProgressController : ControllerBase
{
    private readonly ITestsInProgressService _service;

    public TestsInProgressController(ITestsInProgressService service)
    {
        _service = service;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Missing user id claim.");

    [HttpPost("published-tests/{id:guid}/attempts")]
    public async Task<IActionResult> StartAttempt(Guid id, CancellationToken ct)
    {
        var result = await _service.StartAttemptAsync(CurrentUserId, id, ct);
        return result switch
        {
            StartAttemptResult.Success s => s.AlreadyExisted ? Ok(s.Attempt) : StatusCode(StatusCodes.Status201Created, s.Attempt),
            StartAttemptResult.PublishedTestNotFound => NotFound(),
            StartAttemptResult.TestClosed => BadRequest(new { error = "This test is closed." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpGet("attempts/{id:guid}")]
    public async Task<IActionResult> GetAttempt(Guid id, CancellationToken ct)
    {
        var dto = await _service.GetForStudentAsync(CurrentUserId, id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }
}
