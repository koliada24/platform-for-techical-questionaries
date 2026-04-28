using Api.Contracts;

namespace Api.Services;

public interface ITestTemplatesService
{
    Task<List<TestTemplateSummaryDto>> ListAsync(string teacherId, CancellationToken ct = default);
    Task<TestTemplateDto?> GetAsync(string teacherId, Guid id, CancellationToken ct = default);
    Task<TestTemplateDto> CreateAsync(string teacherId, TestTemplateInput input, CancellationToken ct = default);
    Task<TestTemplateDto?> UpdateAsync(string teacherId, Guid id, TestTemplateInput input, CancellationToken ct = default);
    Task<bool> DeleteAsync(string teacherId, Guid id, CancellationToken ct = default);
    Task<PublishResult> PublishAsync(string teacherId, Guid id, PublishTestTemplateRequest request, CancellationToken ct = default);
}

public abstract record PublishResult
{
    public sealed record Success(IReadOnlyList<TestSummaryDto> Tests) : PublishResult;
    public sealed record TestTemplateNotFound : PublishResult;
    public sealed record UnknownCourses(IReadOnlyList<string> CourseIds) : PublishResult;
    public sealed record ClassroomFailure(string Message) : PublishResult;
}
