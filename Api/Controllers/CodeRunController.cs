using Api.Services.CodeRunner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize]
[Route("api/code")]
public class CodeRunController : ControllerBase
{
    private readonly ICodeRunner _runner;
    private readonly ILogger<CodeRunController> _logger;

    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(5);
    private const int MaxCodeLength = 200_000;

    public CodeRunController(ICodeRunner runner, ILogger<CodeRunController> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    public record RunRequest(string Language, string Code);
    public record RunResponse(
        bool Success,
        string Stdout,
        string? Error,
        bool TimedOut,
        long DurationMs);

    [HttpPost("run")]
    public async Task<ActionResult<RunResponse>> Run([FromBody] RunRequest req, CancellationToken ct)
    {
        if (req is null) return BadRequest(new { error = "Missing body." });
        if (string.IsNullOrWhiteSpace(req.Code))
            return BadRequest(new { error = "Code is required." });
        if (req.Code.Length > MaxCodeLength)
            return BadRequest(new { error = $"Code exceeds maximum length of {MaxCodeLength} characters." });

        var lang = (req.Language ?? "").Trim().ToLowerInvariant();
        if (lang != "csharp" && lang != "c#" && lang != "cs")
            return BadRequest(new { error = $"Language '{req.Language}' is not supported." });

        var result = await _runner.RunCSharpAsync(req.Code, ExecutionTimeout, ct);
        return Ok(new RunResponse(
            result.Success,
            result.Stdout,
            result.Error,
            result.TimedOut,
            result.DurationMs));
    }
}
