namespace Api.Models;

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
    public List<Question> Questions { get; set; } = new();
    public List<TestTemplateAssignment> Assignments { get; set; } = new();
}

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestTemplateId { get; set; }
    public TestTemplate? TestTemplate { get; set; }
    public string Text { get; set; } = null!;
    public int Order { get; set; }
    public List<Answer> Answers { get; set; } = new();
}

public class Answer
{
    public string Text { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}

public class TestTemplateAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestTemplateId { get; set; }
    public TestTemplate? TestTemplate { get; set; }
    public string GoogleCourseId { get; set; } = null!;
    public string GoogleCourseName { get; set; } = null!;
    public DateTimeOffset ClosesAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
