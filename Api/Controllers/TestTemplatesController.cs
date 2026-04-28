using System.Security.Claims;
using Api.Contracts;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/test-templates")]
public class TestTemplatesController : ControllerBase
{
    private readonly ITestTemplatesService _templates;

    public TestTemplatesController(ITestTemplatesService templates)
    {
        _templates = templates;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Missing user id claim.");

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var templates = await _templates.ListAsync(CurrentUserId, ct);
        return Ok(templates);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var template = await _templates.GetAsync(CurrentUserId, id, ct);
        return template is null ? NotFound() : Ok(template);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TestTemplateInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var validation = ValidateInput(input);
        if (validation is not null) return BadRequest(new { error = validation });

        var template = await _templates.CreateAsync(CurrentUserId, input, ct);
        return CreatedAtAction(nameof(Get), new { id = template.Id }, template);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TestTemplateInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var validation = ValidateInput(input);
        if (validation is not null) return BadRequest(new { error = validation });

        var template = await _templates.UpdateAsync(CurrentUserId, id, input, ct);
        return template is null ? NotFound() : Ok(template);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _templates.DeleteAsync(CurrentUserId, id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishTestTemplateRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await _templates.PublishAsync(CurrentUserId, id, request, ct);
        return result switch
        {
            PublishResult.Success s => Ok(s.Tests),
            PublishResult.TestTemplateNotFound => NotFound(),
            PublishResult.UnknownCourses u => BadRequest(new { error = "Unknown course id(s).", unknown = u.CourseIds }),
            PublishResult.ClassroomFailure f => StatusCode(StatusCodes.Status502BadGateway,
                new { error = "Failed to fetch Google Classroom courses.", detail = f.Message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    private static string? ValidateInput(TestTemplateInput input)
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
