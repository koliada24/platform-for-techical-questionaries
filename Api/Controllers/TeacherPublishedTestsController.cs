using System.Security.Claims;
using Api.Contracts;
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

    [HttpGet("attempts/{attemptId:guid}")]
    public async Task<IActionResult> AttemptDetail(Guid attemptId, CancellationToken ct)
    {
        var detail = await _service.GetAttemptDetailForTeacherAsync(CurrentUserId, attemptId, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPut("attempts/{attemptId:guid}/marks")]
    public async Task<IActionResult> SetMarks(
        Guid attemptId,
        [FromBody] SetManualMarksRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _service.SetManualMarksAsync(CurrentUserId, attemptId, request.Marks, ct);
        return result switch
        {
            SetManualMarksResult.Success s => Ok(s.Detail),
            SetManualMarksResult.AttemptNotFound => NotFound(),
            SetManualMarksResult.InvalidMark e => BadRequest(new { error = e.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPost("attempts/{attemptId:guid}/send-mark")]
    public async Task<IActionResult> SendMark(Guid attemptId, CancellationToken ct)
    {
        var result = await _service.SendMarkToClassroomAsync(CurrentUserId, attemptId, ct);
        return result switch
        {
            SendMarkResult.Success s => Ok(new { mark = s.Mark, maxMark = s.MaxMark }),
            SendMarkResult.AttemptNotFound => NotFound(),
            SendMarkResult.NotFullyEvaluated => BadRequest(new
            {
                error = "All questions must be graded before sending the mark to Classroom."
            }),
            SendMarkResult.ClassroomFailure f => StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Failed to send mark to Classroom.",
                detail = f.Message,
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
