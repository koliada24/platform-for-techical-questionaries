namespace Api.Models;

public enum QuestionType
{
    SingleAnswer = 0,
    MultipleAnswers = 1,
    OpenAnswer = 2,
    Code = 3,
    Diagram = 4,
}

public class TestTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TeacherId { get; set; } = null!;
    public ApplicationUser? Teacher { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<QuestionTemplate> Questions { get; set; } = new();
}

public class QuestionTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestTemplateId { get; set; }
    public TestTemplate? TestTemplate { get; set; }
    public string Text { get; set; } = null!;
    public int Order { get; set; }
    public int Mark { get; set; } = 1;
    public QuestionType Type { get; set; } = QuestionType.SingleAnswer;
    /// <summary>
    /// For QuestionType.Code: which language the student writes in (e.g. "python", "javascript").
    /// Drives Monaco syntax highlighting. Null for non-code questions.
    /// </summary>
    public string? CodeLanguage { get; set; }
    public List<Answer> Answers { get; set; } = new();
}

public class Answer
{
    public string Text { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}
