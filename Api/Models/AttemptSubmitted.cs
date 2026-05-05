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
    public int EvaluatedMark { get; set; }
    public bool MarkSent { get; set; }
    public DateTimeOffset? MarkSentAt { get; set; }
    public List<AnswerSubmitted> Answers { get; set; } = new();
}

public abstract class AnswerSubmitted
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptSubmittedId { get; set; }
    public AttemptSubmitted? AttemptSubmitted { get; set; }
    public Guid PublishedQuestionId { get; set; }
    public PublishedQuestion? PublishedQuestion { get; set; }

    /// <summary>
    /// Per-answer awarded mark. Auto-evaluated on submit for Single/Multiple.
    /// Null until the teacher grades it for manual question types (Open/Code/Diagram).
    /// </summary>
    public int? Mark { get; set; }
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
    /// <summary>
    /// Object-storage key for the code text. Content lives in MinIO, not the database.
    /// </summary>
    public string? ObjectKey { get; set; }
}

public class DiagramAnswerSubmitted : AnswerSubmitted
{
    /// <summary>
    /// Object-storage key for the diagram payload. Content lives in MinIO, not the database.
    /// </summary>
    public string? ObjectKey { get; set; }
}
