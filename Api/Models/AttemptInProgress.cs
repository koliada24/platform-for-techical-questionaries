namespace Api.Models;

public class AttemptInProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PublishedTestId { get; set; }
    public PublishedTest? PublishedTest { get; set; }
    public string StudentId { get; set; } = null!;
    public ApplicationUser? Student { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<AnswerInProgress> Answers { get; set; } = new();
}

public abstract class AnswerInProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptInProgressId { get; set; }
    public AttemptInProgress? AttemptInProgress { get; set; }
    public Guid PublishedQuestionId { get; set; }
    public PublishedQuestion? PublishedQuestion { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class SingleAnswerInProgress : AnswerInProgress
{
    public int? SelectedOptionOrder { get; set; }
}

public class MultipleAnswersInProgress : AnswerInProgress
{
    public List<int> SelectedOptionOrders { get; set; } = new();
}

public class TextAnswerInProgress : AnswerInProgress
{
    public string? Text { get; set; }
}
