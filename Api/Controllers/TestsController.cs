using System.Security.Claims;
using Api.Contracts;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/tests")]
public class TestsController : ControllerBase
{
    private readonly ITestsService _tests;

    public TestsController(ITestsService tests)
    {
        _tests = tests;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Missing user id claim.");

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var tests = await _tests.ListAsync(CurrentUserId, ct);
        return Ok(tests);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var test = await _tests.GetAsync(CurrentUserId, id, ct);
        return test is null ? NotFound() : Ok(test);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TestInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var validation = ValidateInput(input);
        if (validation is not null) return BadRequest(new { error = validation });

        var test = await _tests.CreateAsync(CurrentUserId, input, ct);
        return CreatedAtAction(nameof(Get), new { id = test.Id }, test);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TestInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var validation = ValidateInput(input);
        if (validation is not null) return BadRequest(new { error = validation });

        var test = await _tests.UpdateAsync(CurrentUserId, id, input, ct);
        return test is null ? NotFound() : Ok(test);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _tests.DeleteAsync(CurrentUserId, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishTestRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await _tests.PublishAsync(CurrentUserId, id, request, ct);
        return result switch
        {
            PublishResult.Success s => Ok(s.Assignments),
            PublishResult.TestNotFound => NotFound(),
            PublishResult.UnknownCourses u => BadRequest(new { error = "Unknown course id(s).", unknown = u.CourseIds }),
            PublishResult.ClassroomFailure f => StatusCode(StatusCodes.Status502BadGateway,
                new { error = "Failed to fetch Google Classroom courses.", detail = f.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    private static string? ValidateInput(TestInput input)
    {
        if (input.Questions is null || input.Questions.Count == 0)
            return "At least one question is required.";
        for (int i = 0; i < input.Questions.Count; i++)
        {
            var q = input.Questions[i];
            if (string.IsNullOrWhiteSpace(q.Text))
                return $"Question {i + 1} text is required.";
            if (q.Answers is null || q.Answers.Count < 2)
                return $"Question {i + 1} requires at least 2 answers.";
            if (q.Answers.Any(a => string.IsNullOrWhiteSpace(a.Text)))
                return $"Question {i + 1} has an empty answer.";
            if (!q.Answers.Any(a => a.IsCorrect))
                return $"Question {i + 1} must have at least one correct answer.";
        }
        return null;
    }
}
