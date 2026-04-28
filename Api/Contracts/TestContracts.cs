using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public record TestSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    string GoogleCourseId,
    string GoogleCourseName,
    DateTimeOffset ClosesAt,
    DateTimeOffset CreatedAt
);
public record TestForStudentDto(
    Guid Id,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    DateTimeOffset ClosesAt,
    List<TestQuestionForStudentDto> Questions
);

public record TestQuestionForStudentDto(
    Guid Id,
    string Text,
    int Order,
    List<TestAnswerOptionForStudentDto> Options
);

public record TestAnswerOptionForStudentDto(Guid Id, string Text, int Order);

public record TestInProcessDto(
    Guid Id,
    Guid TestId,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    List<TestSelectionDto> Selections
);

public record TestSelectionDto(Guid TestQuestionId, Guid TestAnswerOptionId);

public record SaveSelectionsRequest(
    [Required] List<TestSelectionDto> Selections
);

public record TestAnswersDto(
    Guid Id,
    Guid TestId,
    DateTimeOffset StartedAt,
    DateTimeOffset SubmittedAt,
    int CorrectCount,
    int QuestionCount,
    List<TestSelectionDto> Selections
);
