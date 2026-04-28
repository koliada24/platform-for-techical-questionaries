using Api.Contracts;

namespace Api.Services;

public interface ITestsService
{
    Task<TestForStudentDto?> GetForStudentAsync(Guid testId, CancellationToken ct = default);

    Task<StartTestResult> StartAsync(string studentId, Guid testId, CancellationToken ct = default);

    Task<TestInProcessDto?> GetInProcessAsync(string studentId, Guid inProcessId, CancellationToken ct = default);

    Task<TestInProcessDto?> SaveSelectionsAsync(string studentId, Guid inProcessId, List<TestSelectionDto> selections, CancellationToken ct = default);

    Task<SubmitTestResult> SubmitAsync(string studentId, Guid inProcessId, CancellationToken ct = default);
}

public abstract record StartTestResult
{
    public sealed record Success(TestInProcessDto InProcess, bool AlreadyExisted) : StartTestResult;
    public sealed record TestNotFound : StartTestResult;
    public sealed record TestClosed : StartTestResult;
    public sealed record AlreadySubmitted : StartTestResult;
}

public abstract record SubmitTestResult
{
    public sealed record Success(TestAnswersDto Answers) : SubmitTestResult;
    public sealed record InProcessNotFound : SubmitTestResult;
}
