namespace Api.Contracts;

public record PublishedTestInfoDto(
    System.Guid Id,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    int QuestionCount,
    System.DateTimeOffset ClosesAt
);

public record PublishedTestListItemDto(
    System.Guid TestTemplateId,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    int QuestionCount,
    int CourseCount,
    System.DateTimeOffset OpenedAt,
    System.DateTimeOffset ClosesAt
);

public record SubmittedAttemptSummaryDto(
    System.Guid Id,
    string StudentId,
    string? StudentName,
    string? StudentEmail,
    string? StudentPictureUrl,
    System.DateTimeOffset StartedAt,
    System.DateTimeOffset SubmittedAt,
    long DurationSeconds,
    int EvaluatedMark,
    bool IsEvaluated
);

public record PublishedTestDetailDto(
    System.Guid TestTemplateId,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    int QuestionCount,
    int MaxMark,
    int CourseCount,
    System.DateTimeOffset OpenedAt,
    System.DateTimeOffset ClosesAt,
    System.Collections.Generic.List<SubmittedAttemptSummaryDto> SubmittedAttempts
);
