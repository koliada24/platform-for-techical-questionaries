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

    Task<SaveAnswerResult> SaveSingleAnswerAsync(string studentId, Guid attemptId, Guid questionId, SaveSingleAnswerInput input, CancellationToken ct = default);
    Task<SaveAnswerResult> SaveMultipleAnswersAsync(string studentId, Guid attemptId, Guid questionId, SaveMultipleAnswersInput input, CancellationToken ct = default);
    Task<SaveAnswerResult> SaveTextAnswerAsync(string studentId, Guid attemptId, Guid questionId, SaveTextAnswerInput input, CancellationToken ct = default);
    Task<SaveAnswerResult> SaveCodeAnswerAsync(string studentId, Guid attemptId, Guid questionId, SaveCodeAnswerInput input, CancellationToken ct = default);
    Task<SaveAnswerResult> SaveDiagramAnswerAsync(string studentId, Guid attemptId, Guid questionId, SaveDiagramAnswerInput input, CancellationToken ct = default);

    Task<ClearAnswerResult> ClearAnswerAsync(string studentId, Guid attemptId, Guid questionId, CancellationToken ct = default);

    Task<SubmitAttemptResult> SubmitAttemptAsync(string studentId, Guid attemptId, CancellationToken ct = default);
}

public abstract record SubmitAttemptResult
{
    public sealed record Success(Guid SubmittedAttemptId) : SubmitAttemptResult;
    public sealed record AttemptNotFound : SubmitAttemptResult;
}

public abstract record StartAttemptResult
{
    public sealed record Success(AttemptInProgressDto Attempt, bool AlreadyExisted) : StartAttemptResult;
    public sealed record PublishedTestNotFound : StartAttemptResult;
    public sealed record TestClosed : StartAttemptResult;
}

public abstract record SaveAnswerResult
{
    public sealed record Success : SaveAnswerResult;
    public sealed record AttemptNotFound : SaveAnswerResult;
    public sealed record QuestionNotFound : SaveAnswerResult;
    public sealed record WrongQuestionType(string Expected, string Actual) : SaveAnswerResult;
}

public abstract record ClearAnswerResult
{
    public sealed record Success : ClearAnswerResult;
    public sealed record AttemptNotFound : ClearAnswerResult;
}
