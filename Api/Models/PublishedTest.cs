namespace Api.Models;

public class PublishedTest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestTemplateId { get; set; }
    public string TeacherId { get; set; } = null!;
    public ApplicationUser? Teacher { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public string GoogleCourseId { get; set; } = null!;
    public string GoogleCourseName { get; set; } = null!;
    public string? GoogleCourseWorkId { get; set; }
    public string? GoogleCourseWorkLink { get; set; }
    public DateTimeOffset ClosesAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<PublishedQuestion> Questions { get; set; } = new();
}

public class PublishedQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PublishedTestId { get; set; }
    public PublishedTest? PublishedTest { get; set; }
    public string Text { get; set; } = null!;
    public int Order { get; set; }
    public QuestionType Type { get; set; } = QuestionType.SingleAnswer;
    public List<Answer> Answers { get; set; } = new();
}
