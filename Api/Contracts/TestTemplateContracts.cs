using Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Api.Contracts;

public record AnswerDto(string Text, bool IsCorrect, int Order);

public record QuestionTemplateDto(Guid Id, string Text, int Order, QuestionType Type, List<AnswerDto> Answers);

public record TestTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<QuestionTemplateDto> Questions
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

public class AnswerInput
{
    [Required]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; } = false;

    public int Order { get; set; } = 0;

    public Answer ToAnswer()
    {
        return new Answer
        {
            Text = Text,
            IsCorrect = IsCorrect,
            Order = Order
        };
    }
}

public record QuestionTemplateInput(
    Guid? Id,
    [Required, MinLength(1)] string Text,
    int Order,
    QuestionType Type,
    List<AnswerInput> Answers
);

public record TestTemplateInput(
    [Required, MinLength(1), MaxLength(200)] string Name,
    [MaxLength(2000)] string? Description,
    [Range(1, 600)] int? TimeLimitMinutes,
    [MinLength(1)] List<QuestionTemplateInput> Questions
);

// Google Classroom
public record ClassroomCourseDto(string Id, string Name, string? Section, string? Description);

public record PublishTestTemplateRequest(
    [Required, MinLength(1)] List<string> CourseIds,
    [Required] DateTimeOffset ClosesAt
);
