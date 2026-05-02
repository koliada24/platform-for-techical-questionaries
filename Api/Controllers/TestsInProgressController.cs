using System.Security.Claims;
using Api.Contracts;
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

    [HttpPut("attempts/{attemptId:guid}/questions/{questionId:guid}/single-answer")]
    public Task<IActionResult> SaveSingleAnswer(Guid attemptId, Guid questionId, [FromBody] SaveSingleAnswerInput input, CancellationToken ct)
        => MapSaveResult(_service.SaveSingleAnswerAsync(CurrentUserId, attemptId, questionId, input, ct));

    [HttpPut("attempts/{attemptId:guid}/questions/{questionId:guid}/multiple-answers")]
    public Task<IActionResult> SaveMultipleAnswers(Guid attemptId, Guid questionId, [FromBody] SaveMultipleAnswersInput input, CancellationToken ct)
        => MapSaveResult(_service.SaveMultipleAnswersAsync(CurrentUserId, attemptId, questionId, input, ct));

    [HttpPut("attempts/{attemptId:guid}/questions/{questionId:guid}/text-answer")]
    public Task<IActionResult> SaveTextAnswer(Guid attemptId, Guid questionId, [FromBody] SaveTextAnswerInput input, CancellationToken ct)
        => MapSaveResult(_service.SaveTextAnswerAsync(CurrentUserId, attemptId, questionId, input, ct));

    [HttpPut("attempts/{attemptId:guid}/questions/{questionId:guid}/code-answer")]
    public Task<IActionResult> SaveCodeAnswer(Guid attemptId, Guid questionId, [FromBody] SaveCodeAnswerInput input, CancellationToken ct)
        => MapSaveResult(_service.SaveCodeAnswerAsync(CurrentUserId, attemptId, questionId, input, ct));

    [HttpPut("attempts/{attemptId:guid}/questions/{questionId:guid}/diagram-answer")]
    public Task<IActionResult> SaveDiagramAnswer(Guid attemptId, Guid questionId, [FromBody] SaveDiagramAnswerInput input, CancellationToken ct)
        => MapSaveResult(_service.SaveDiagramAnswerAsync(CurrentUserId, attemptId, questionId, input, ct));

    [HttpDelete("attempts/{attemptId:guid}/questions/{questionId:guid}/answer")]
    public async Task<IActionResult> ClearAnswer(Guid attemptId, Guid questionId, CancellationToken ct)
    {
        var result = await _service.ClearAnswerAsync(CurrentUserId, attemptId, questionId, ct);
        return result switch
        {
            ClearAnswerResult.Success => NoContent(),
            ClearAnswerResult.AttemptNotFound => NotFound(new { error = "Attempt not found." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpPost("attempts/{attemptId:guid}/submit")]
    public async Task<IActionResult> SubmitAttempt(Guid attemptId, CancellationToken ct)
    {
        var result = await _service.SubmitAttemptAsync(CurrentUserId, attemptId, ct);
        return result switch
        {
            SubmitAttemptResult.Success s => Ok(new { submittedAttemptId = s.SubmittedAttemptId }),
            SubmitAttemptResult.AttemptNotFound => NotFound(new { error = "Attempt not found." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    private async Task<IActionResult> MapSaveResult(Task<SaveAnswerResult> task)
    {
        var result = await task;
        return result switch
        {
            SaveAnswerResult.Success => NoContent(),
            SaveAnswerResult.AttemptNotFound => NotFound(new { error = "Attempt not found." }),
            SaveAnswerResult.QuestionNotFound => NotFound(new { error = "Question not found." }),
            SaveAnswerResult.WrongQuestionType w => BadRequest(new { error = $"Wrong question type. Expected {w.Expected}, got {w.Actual}." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
