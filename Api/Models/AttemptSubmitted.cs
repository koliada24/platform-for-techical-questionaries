namespace Api.Models;

public class AttemptSubmitted
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PublishedTestId { get; set; }
    public PublishedTest? PublishedTest { get; set; }
    public string StudentId { get; set; } = null!;
    public ApplicationUser? Student { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public long DurationSeconds { get; set; }
    public List<AnswerSubmitted> Answers { get; set; } = new();
}

public abstract class AnswerSubmitted
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptSubmittedId { get; set; }
    public AttemptSubmitted? AttemptSubmitted { get; set; }
    public Guid PublishedQuestionId { get; set; }
    public PublishedQuestion? PublishedQuestion { get; set; }
}

public class SingleAnswerSubmitted : AnswerSubmitted
{
    public int? SelectedOptionOrder { get; set; }
}

public class MultipleAnswersSubmitted : AnswerSubmitted
{
    public List<int> SelectedOptionOrders { get; set; } = new();
}

public class TextAnswerSubmitted : AnswerSubmitted
{
    public string? Text { get; set; }
}

public class CodeAnswerSubmitted : AnswerSubmitted
{
    public string? Text { get; set; }
}

public class DiagramAnswerSubmitted : AnswerSubmitted
{
    public string? Text { get; set; }
}
