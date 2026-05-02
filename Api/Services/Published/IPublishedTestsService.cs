using Api.Contracts;

namespace Api.Services.Published;

public interface IPublishedTestsService
{
    Task<PublishedTestInfoDto?> GetInfoAsync(Guid id, CancellationToken ct = default);

    Task<List<PublishedTestListItemDto>> ListForTeacherAsync(string teacherId, CancellationToken ct = default);

    Task<PublishedTestDetailDto?> GetDetailForTeacherAsync(
        string teacherId,
        Guid testTemplateId,
        DateTimeOffset closesAt,
        CancellationToken ct = default);

    Task<AttemptDetailForTeacherDto?> GetAttemptDetailForTeacherAsync(
        string teacherId,
        Guid attemptId,
        CancellationToken ct = default);

    Task<SetManualMarksResult> SetManualMarksAsync(
        string teacherId,
        Guid attemptId,
        List<SetManualMarkInput> marks,
        CancellationToken ct = default);

    Task<SendMarkResult> SendMarkToClassroomAsync(
        string teacherId,
        Guid attemptId,
        CancellationToken ct = default);
}

public abstract record SetManualMarksResult
{
    public sealed record AttemptNotFound : SetManualMarksResult;
    public sealed record InvalidMark(string Message) : SetManualMarksResult;
    public sealed record Success(AttemptDetailForTeacherDto Detail) : SetManualMarksResult;
}

public abstract record SendMarkResult
{
    public sealed record AttemptNotFound : SendMarkResult;
    public sealed record NotFullyEvaluated : SendMarkResult;
    public sealed record ClassroomFailure(string Message) : SendMarkResult;
    public sealed record Success(int Mark, int MaxMark) : SendMarkResult;
}
