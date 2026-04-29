using Api.Contracts;

namespace Api.Services.InProgress;

public interface ITestsInProgressService
{
    
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
