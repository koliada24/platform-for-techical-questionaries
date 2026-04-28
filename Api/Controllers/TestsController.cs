using System.Security.Claims;
using Api.Contracts;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/tests")]
public class TestsController : ControllerBase
{
    private readonly ITestsService _testsService;

    public TestsController(ITestsService testsService)
    {
        _testsService = testsService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Missing user id claim.");

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var dto = await _testsService.GetForStudentAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct)
    {
        var result = await _testsService.StartAsync(CurrentUserId, id, ct);
        return result switch
        {
            StartTestResult.Success s => Ok(s.InProcess),
            StartTestResult.TestNotFound => NotFound(),
            StartTestResult.TestClosed => Conflict(new { error = "Test is closed." }),
            StartTestResult.AlreadySubmitted => Conflict(new { error = "Test already submitted." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpGet("in-process/{inProcessId:guid}")]
    public async Task<IActionResult> GetInProcess(Guid inProcessId, CancellationToken ct)
    {
        var dto = await _testsService.GetInProcessAsync(CurrentUserId, inProcessId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("in-process/{inProcessId:guid}/selections")]
    public async Task<IActionResult> SaveSelections(Guid inProcessId, [FromBody] SaveSelectionsRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var dto = await _testsService.SaveSelectionsAsync(CurrentUserId, inProcessId, request.Selections, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("in-process/{inProcessId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid inProcessId, CancellationToken ct)
    {
        var result = await _testsService.SubmitAsync(CurrentUserId, inProcessId, ct);
        return result switch
        {
            SubmitTestResult.Success s => Ok(s.Answers),
            SubmitTestResult.InProcessNotFound => NotFound(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
