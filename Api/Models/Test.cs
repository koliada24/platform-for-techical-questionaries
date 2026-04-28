namespace Api.Models;

public class Test
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string TeacherId { get; set; } = null!;
    public ApplicationUser? Teacher { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? TimeLimitMinutes { get; set; }

    public string GoogleCourseId { get; set; } = null!;
    public string GoogleCourseName { get; set; } = null!;

    /// <summary>Google Classroom courseWork id (assignment) created for this test. Null if not yet created.</summary>
    public string? GoogleCourseWorkId { get; set; }

    /// <summary>Public link to the assignment in Google Classroom (alternateLink).</summary>
    public string? GoogleCourseWorkLink { get; set; }

    public DateTimeOffset ClosesAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<TestQuestion> Questions { get; set; } = new();
}

public class TestQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestId { get; set; }
    public Test? Test { get; set; }

    public string Text { get; set; } = null!;
    public int Order { get; set; }

    public List<TestAnswerOption> Options { get; set; } = new();
}

public class TestAnswerOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestQuestionId { get; set; }
    public TestQuestion? Question { get; set; }

    public string Text { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int Order { get; set; }
}

public class TestInProcess
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TestId { get; set; }
    public Test? Test { get; set; }

    public string StudentId { get; set; } = null!;
    public ApplicationUser? Student { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<TestInProcessAnswer> Selections { get; set; } = new();
}

public class TestInProcessAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TestInProcessId { get; set; }
    public TestInProcess? TestInProcess { get; set; }

    public Guid TestQuestionId { get; set; }
    public Guid TestAnswerOptionId { get; set; }
}

public class TestAnswers
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TestId { get; set; }
    public Test? Test { get; set; }

    public string StudentId { get; set; } = null!;
    public ApplicationUser? Student { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;

    public int CorrectCount { get; set; }
    public int QuestionCount { get; set; }

    public List<TestAnswerSelection> Selections { get; set; } = new();
}

public class TestAnswerSelection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TestAnswersId { get; set; }
    public TestAnswers? TestAnswers { get; set; }

    public Guid TestQuestionId { get; set; }
    public Guid TestAnswerOptionId { get; set; }
}
