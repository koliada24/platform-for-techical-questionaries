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

public record AttemptAnswerOptionDto(int Order, string Text, bool IsCorrect);

public record AttemptQuestionForTeacherDto(
    System.Guid PublishedQuestionId,
    string Text,
    int Order,
    int MaxMark,
    Api.Models.QuestionType Type,
    System.Collections.Generic.List<AttemptAnswerOptionDto> Options,
    int? SelectedOptionOrder,
    System.Collections.Generic.List<int>? SelectedOptionOrders,
    string? AnswerText,
    int? Mark,
    bool IsAutoEvaluated
);

public record AttemptDetailForTeacherDto(
    System.Guid AttemptId,
    System.Guid TestTemplateId,
    System.DateTimeOffset ClosesAt,
    string TestName,
    string StudentId,
    string? StudentName,
    string? StudentEmail,
    string? StudentPictureUrl,
    System.DateTimeOffset StartedAt,
    System.DateTimeOffset SubmittedAt,
    long DurationSeconds,
    int MaxMark,
    int TotalMark,
    bool IsFullyEvaluated,
    bool MarkSent,
    System.Collections.Generic.List<AttemptQuestionForTeacherDto> Questions
);

public record SetManualMarkInput(
    [System.ComponentModel.DataAnnotations.Required] System.Guid PublishedQuestionId,
    int? Mark
);

public record SetManualMarksRequest(
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(1)]
    System.Collections.Generic.List<SetManualMarkInput> Marks
);
