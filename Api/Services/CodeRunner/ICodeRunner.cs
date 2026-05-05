namespace Api.Services.CodeRunner;

public record CodeRunResult(
    bool Success,
    string Stdout,
    string? Error,
    bool TimedOut,
    long DurationMs);

public interface ICodeRunner
{
    /// <summary>
    /// Compiles and runs the supplied C# source. Captures stdout written via Console.Write/WriteLine.
    /// Enforces a hard wall-clock timeout. Compile errors and runtime exceptions are returned in
    /// <see cref="CodeRunResult.Error"/> with <see cref="CodeRunResult.Success"/> = false.
    /// </summary>
    Task<CodeRunResult> RunCSharpAsync(string code, TimeSpan timeout, CancellationToken ct = default);
}
