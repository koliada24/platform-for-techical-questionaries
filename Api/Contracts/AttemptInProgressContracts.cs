using Api.Models;

namespace Api.Contracts;

public record AttemptInProgressDto(
    System.Guid Id,
    System.Guid PublishedTestId,
    System.DateTimeOffset StartedAt
);

public record AnswerOptionForStudentDto(int Order, string Text);

public record AttemptQuestionForStudentDto(
    System.Guid Id,
    string Text,
    int Order,
    QuestionType Type,
    System.Collections.Generic.List<AnswerOptionForStudentDto> Options
);

public record AttemptForStudentDto(
    System.Guid Id,
    System.Guid PublishedTestId,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    System.DateTimeOffset StartedAt,
    System.DateTimeOffset ClosesAt,
    System.Collections.Generic.List<AttemptQuestionForStudentDto> Questions
);
