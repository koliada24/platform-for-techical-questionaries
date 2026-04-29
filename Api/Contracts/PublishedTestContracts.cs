namespace Api.Contracts;

public record PublishedTestInfoDto(
    System.Guid Id,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    int QuestionCount,
    System.DateTimeOffset ClosesAt
);
