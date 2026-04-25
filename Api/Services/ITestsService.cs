using Api.Contracts;

namespace Api.Services;

public interface ITestsService
{
    Task<List<TestSummaryDto>> ListAsync(string teacherId, CancellationToken ct = default);
    Task<TestDto?> GetAsync(string teacherId, Guid id, CancellationToken ct = default);
    Task<TestDto> CreateAsync(string teacherId, TestInput input, CancellationToken ct = default);
    Task<TestDto?> UpdateAsync(string teacherId, Guid id, TestInput input, CancellationToken ct = default);
    Task<bool> DeleteAsync(string teacherId, Guid id, CancellationToken ct = default);
    Task<PublishResult> PublishAsync(string teacherId, Guid id, PublishTestRequest request, CancellationToken ct = default);
}

public abstract record PublishResult
{
    public sealed record Success(IReadOnlyList<TestAssignmentDto> Assignments) : PublishResult;
    public sealed record TestNotFound : PublishResult;
    public sealed record UnknownCourses(IReadOnlyList<string> CourseIds) : PublishResult;
    public sealed record ClassroomFailure(string Message) : PublishResult;
}
