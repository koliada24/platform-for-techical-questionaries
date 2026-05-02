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
