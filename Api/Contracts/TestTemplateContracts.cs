using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public record AnswerDto(string Text, bool IsCorrect, int Order);

public record QuestionDto(Guid Id, string Text, int Order, List<AnswerDto> Answers);

public record TestTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<QuestionDto> Questions
);

public record TestTemplateSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    int QuestionCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record AnswerInput(
    [Required] string Text,
    bool IsCorrect
);

public record QuestionInput(
    Guid? Id,
    [Required, MinLength(1)] string Text,
    int Order,
    [MinLength(2)] List<AnswerInput> Answers
);

public record TestTemplateInput(
    [Required, MinLength(1), MaxLength(200)] string Name,
    [MaxLength(2000)] string? Description,
    [Range(1, 600)] int? TimeLimitMinutes,
    [MinLength(1)] List<QuestionInput> Questions
);

// Google Classroom
public record ClassroomCourseDto(string Id, string Name, string? Section, string? Description);

public record PublishTestTemplateRequest(
    [Required, MinLength(1)] List<string> CourseIds,
    [Required] DateTimeOffset ClosesAt
);

public record TestTemplateAssignmentDto(
    Guid Id,
    string GoogleCourseId,
    string GoogleCourseName,
    DateTimeOffset ClosesAt,
    DateTimeOffset CreatedAt
);
