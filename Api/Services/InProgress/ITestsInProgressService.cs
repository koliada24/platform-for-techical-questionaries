using Api.Contracts;

namespace Api.Services.InProgress;

public interface ITestsInProgressService
{
    Task<StartAttemptResult> StartAttemptAsync(
        string studentId,
        Guid publishedTestId,
        CancellationToken ct = default);

    Task<AttemptForStudentDto?> GetForStudentAsync(
        string studentId,
        Guid attemptId,
        CancellationToken ct = default);
}

public abstract record StartAttemptResult
{
    public sealed record Success(AttemptInProgressDto Attempt, bool AlreadyExisted) : StartAttemptResult;
    public sealed record PublishedTestNotFound : StartAttemptResult;
    public sealed record TestClosed : StartAttemptResult;
}
