using Api.Contracts;
using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.InProgress;

public class TestsInProgressService : ITestsInProgressService
{
    private readonly AppDbContext _db;
    private readonly GoogleClassroomClient _classroom;
    private readonly ILogger<TestsInProgressService> _logger;

    public TestsInProgressService(
        AppDbContext db,
        GoogleClassroomClient classroom,
        ILogger<TestsInProgressService> logger)
    {
        _db = db;
        _classroom = classroom;
        _logger = logger;
    }

    public async Task<StartAttemptResult> StartAttemptAsync(
        string studentId,
        Guid publishedTestId,
        CancellationToken ct = default)
    {
        var test = await _db.PublishedTests
            .FirstOrDefaultAsync(t => t.Id == publishedTestId, ct);
        if (test is null)
        {
            return new StartAttemptResult.PublishedTestNotFound();
        }

        if (test.ClosesAt <= DateTimeOffset.UtcNow)
        {
            return new StartAttemptResult.TestClosed();
        }

        var existing = await _db.AttemptsInProgress
            .FirstOrDefaultAsync(a => a.StudentId == studentId && a.PublishedTestId == publishedTestId, ct);
        if (existing is not null)
        {
            return new StartAttemptResult.Success(ToDto(existing), AlreadyExisted: true);
        }

        var attempt = new AttemptInProgress
        {
            PublishedTestId = publishedTestId,
            StudentId = studentId,
        };

        _db.AttemptsInProgress.Add(attempt);
        await _db.SaveChangesAsync(ct);

        return new StartAttemptResult.Success(ToDto(attempt), AlreadyExisted: false);
    }

    private static AttemptInProgressDto ToDto(AttemptInProgress a) =>
        new(a.Id, a.PublishedTestId, a.StartedAt);

    private static SavedAnswerDto ToSavedAnswerDto(AnswerInProgress a) => a switch
    {
        SingleAnswerInProgress s => new SavedAnswerDto(QuestionType.SingleAnswer, s.SelectedOptionOrder, null, null),
        MultipleAnswersInProgress m => new SavedAnswerDto(QuestionType.MultipleAnswers, null, m.SelectedOptionOrders.ToList(), null),
        TextAnswerInProgress t => new SavedAnswerDto(QuestionType.OpenAnswer, null, null, t.Text),
        CodeAnswerInProgress c => new SavedAnswerDto(QuestionType.Code, null, null, c.Text),
        DiagramAnswerInProgress d => new SavedAnswerDto(QuestionType.Diagram, null, null, d.Text),
        _ => throw new InvalidOperationException($"Unknown answer subtype: {a.GetType().Name}"),
    };

    public async Task<AttemptForStudentDto?> GetForStudentAsync(
        string studentId,
        Guid attemptId,
        CancellationToken ct = default)
    {
        var attempt = await _db.AttemptsInProgress
            .Include(a => a.PublishedTest)
                .ThenInclude(t => t!.Questions)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId, ct);

        if (attempt is null || attempt.PublishedTest is null) return null;

        var t = attempt.PublishedTest;

        var savedAnswers = await _db.AnswersInProgress
            .Where(a => a.AttemptInProgressId == attempt.Id)
            .ToListAsync(ct);
        var savedByQuestion = savedAnswers.ToDictionary(a => a.PublishedQuestionId, ToSavedAnswerDto);

        var questions = t.Questions
            .OrderBy(q => q.Order)
            .Select(q => new AttemptQuestionForStudentDto(
                q.Id,
                q.Text,
                q.Order,
                q.Type,
                q.CodeLanguage,
                q.Answers
                    .OrderBy(a => a.Order)
                    .Select(a => new AnswerOptionForStudentDto(a.Order, a.Text))
                    .ToList(),
                savedByQuestion.TryGetValue(q.Id, out var saved) ? saved : null))
            .ToList();

        return new AttemptForStudentDto(
            attempt.Id,
            t.Id,
            t.Name,
            t.Description,
            t.TimeLimitMinutes,
            attempt.StartedAt,
            t.ClosesAt,
            questions);
    }

    public Task<SaveAnswerResult> SaveSingleAnswerAsync(string studentId, Guid attemptId, Guid questionId, SaveSingleAnswerInput input, CancellationToken ct = default) =>
        SaveAnswerAsync(studentId, attemptId, questionId, QuestionType.SingleAnswer,
            () => new SingleAnswerInProgress { SelectedOptionOrder = input.SelectedOptionOrder },
            ct);

    public Task<SaveAnswerResult> SaveMultipleAnswersAsync(string studentId, Guid attemptId, Guid questionId, SaveMultipleAnswersInput input, CancellationToken ct = default) =>
        SaveAnswerAsync(studentId, attemptId, questionId, QuestionType.MultipleAnswers,
            () => new MultipleAnswersInProgress
            {
                SelectedOptionOrders = (input.SelectedOptionOrders ?? new List<int>())
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList()
            },
            ct);

    public Task<SaveAnswerResult> SaveTextAnswerAsync(string studentId, Guid attemptId, Guid questionId, SaveTextAnswerInput input, CancellationToken ct = default) =>
        SaveAnswerAsync(studentId, attemptId, questionId, QuestionType.OpenAnswer,
            () => new TextAnswerInProgress { Text = input.Text },
            ct);

    public Task<SaveAnswerResult> SaveCodeAnswerAsync(string studentId, Guid attemptId, Guid questionId, SaveCodeAnswerInput input, CancellationToken ct = default) =>
        SaveAnswerAsync(studentId, attemptId, questionId, QuestionType.Code,
            () => new CodeAnswerInProgress { Text = input.Text },
            ct);

    public Task<SaveAnswerResult> SaveDiagramAnswerAsync(string studentId, Guid attemptId, Guid questionId, SaveDiagramAnswerInput input, CancellationToken ct = default) =>
        SaveAnswerAsync(studentId, attemptId, questionId, QuestionType.Diagram,
            () => new DiagramAnswerInProgress { Text = input.Text },
            ct);

    public async Task<ClearAnswerResult> ClearAnswerAsync(string studentId, Guid attemptId, Guid questionId, CancellationToken ct = default)
    {
        var attempt = await _db.AttemptsInProgress
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId, ct);
        if (attempt is null)
        {
            return new ClearAnswerResult.AttemptNotFound();
        }

        var existing = await _db.AnswersInProgress
            .FirstOrDefaultAsync(a => a.AttemptInProgressId == attemptId && a.PublishedQuestionId == questionId, ct);
        if (existing is not null)
        {
            _db.AnswersInProgress.Remove(existing);
            attempt.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return new ClearAnswerResult.Success();
    }

    private async Task<SaveAnswerResult> SaveAnswerAsync(
        string studentId,
        Guid attemptId,
        Guid questionId,
        QuestionType expectedType,
        Func<AnswerInProgress> factory,
        CancellationToken ct)
    {
        var attempt = await _db.AttemptsInProgress
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId, ct);
        if (attempt is null)
        {
            return new SaveAnswerResult.AttemptNotFound();
        }

        var question = await _db.PublishedQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.PublishedTestId == attempt.PublishedTestId, ct);
        if (question is null)
        {
            return new SaveAnswerResult.QuestionNotFound();
        }

        if (question.Type != expectedType)
        {
            return new SaveAnswerResult.WrongQuestionType(expectedType.ToString(), question.Type.ToString());
        }

        // Replace the existing answer for this question (TPH discriminator can't be updated in place).
        var existing = await _db.AnswersInProgress
            .FirstOrDefaultAsync(a => a.AttemptInProgressId == attemptId && a.PublishedQuestionId == questionId, ct);
        if (existing is not null)
        {
            _db.AnswersInProgress.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }

        var answer = factory();
        answer.AttemptInProgressId = attemptId;
        answer.PublishedQuestionId = questionId;
        answer.UpdatedAt = DateTimeOffset.UtcNow;

        _db.AnswersInProgress.Add(answer);
        attempt.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new SaveAnswerResult.Success();
    }

    public async Task<SubmitAttemptResult> SubmitAttemptAsync(string studentId, Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await _db.AttemptsInProgress
            .Include(a => a.Answers)
            .Include(a => a.PublishedTest)
            .Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId, ct);
        if (attempt is null)
        {
            return new SubmitAttemptResult.AttemptNotFound();
        }

        var submittedAt = DateTimeOffset.UtcNow;
        var duration = (long)Math.Max(0, (submittedAt - attempt.StartedAt).TotalSeconds);

        var questions = await _db.PublishedQuestions
            .Where(q => q.PublishedTestId == attempt.PublishedTestId)
            .ToListAsync(ct);
        var questionsById = questions.ToDictionary(q => q.Id);

        var submittedAnswers = attempt.Answers
            .Select(a => ToSubmitted(a, questionsById))
            .ToList();

        // Ensure every question has a submitted-answer row, even if the student left it blank.
        // Without this, the teacher has nothing to attach a manual mark to.
        var answeredQuestionIds = submittedAnswers.Select(a => a.PublishedQuestionId).ToHashSet();
        foreach (var question in questions)
        {
            if (answeredQuestionIds.Contains(question.Id)) continue;
            var blank = CreateBlankSubmitted(question);
            // Auto-evaluated types: a blank choice answer is worth 0.
            if (blank is SingleAnswerSubmitted or MultipleAnswersSubmitted)
            {
                blank.Mark = 0;
            }
            submittedAnswers.Add(blank);
        }

        var evaluatedMark = submittedAnswers.Sum(a => a.Mark ?? 0);

        var submitted = new AttemptSubmitted
        {
            PublishedTestId = attempt.PublishedTestId,
            StudentId = attempt.StudentId,
            StartedAt = attempt.StartedAt,
            SubmittedAt = submittedAt,
            DurationSeconds = duration,
            EvaluatedMark = evaluatedMark,
            Answers = submittedAnswers,
        };

        var publishedTest = attempt.PublishedTest;
        var student = attempt.Student;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.AttemptsSubmitted.Add(submitted);
        _db.AnswersInProgress.RemoveRange(attempt.Answers);
        _db.AttemptsInProgress.Remove(attempt);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Best-effort: mark the Classroom assignment as turned in for this student.
        if (student is not null
            && publishedTest is not null
            && !string.IsNullOrEmpty(publishedTest.GoogleCourseId)
            && !string.IsNullOrEmpty(publishedTest.GoogleCourseWorkId))
        {
            try
            {
                await _classroom.TurnInStudentSubmissionAsync(
                    student,
                    publishedTest.GoogleCourseId,
                    publishedTest.GoogleCourseWorkId!,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to turn in Classroom submission for student {StudentId} on courseWork {CourseWorkId}.",
                    studentId, publishedTest.GoogleCourseWorkId);
            }
        }

        return new SubmitAttemptResult.Success(submitted.Id);
    }

    private static int EvaluateAnswer(AnswerInProgress answer, Dictionary<Guid, PublishedQuestion> questionsById)
    {
        if (!questionsById.TryGetValue(answer.PublishedQuestionId, out var question))
        {
            return 0;
        }

        switch (answer)
        {
            case SingleAnswerInProgress s:
            {
                if (s.SelectedOptionOrder is null) return 0;
                var correct = question.Answers.FirstOrDefault(a => a.IsCorrect);
                if (correct is null) return 0;
                return correct.Order == s.SelectedOptionOrder.Value ? question.Mark : 0;
            }
            case MultipleAnswersInProgress m:
            {
                var correctOrders = question.Answers
                    .Where(a => a.IsCorrect)
                    .Select(a => a.Order)
                    .ToHashSet();
                if (correctOrders.Count == 0) return 0;

                var selected = m.SelectedOptionOrders.Distinct().ToList();
                var correctSelected = selected.Count(o => correctOrders.Contains(o));

                // Validation guarantees Mark is divisible by correctOrders.Count.
                var unit = question.Mark / correctOrders.Count;
                return unit * correctSelected;
            }
            default:
                return 0;
        }
    }

    private static AnswerSubmitted ToSubmitted(AnswerInProgress a, Dictionary<Guid, PublishedQuestion> questionsById)
    {
        AnswerSubmitted submitted = a switch
        {
            SingleAnswerInProgress s => new SingleAnswerSubmitted
            {
                PublishedQuestionId = s.PublishedQuestionId,
                SelectedOptionOrder = s.SelectedOptionOrder,
            },
            MultipleAnswersInProgress m => new MultipleAnswersSubmitted
            {
                PublishedQuestionId = m.PublishedQuestionId,
                SelectedOptionOrders = m.SelectedOptionOrders.ToList(),
            },
            TextAnswerInProgress t => new TextAnswerSubmitted
            {
                PublishedQuestionId = t.PublishedQuestionId,
                Text = t.Text,
            },
            CodeAnswerInProgress c => new CodeAnswerSubmitted
            {
                PublishedQuestionId = c.PublishedQuestionId,
                Text = c.Text,
            },
            DiagramAnswerInProgress d => new DiagramAnswerSubmitted
            {
                PublishedQuestionId = d.PublishedQuestionId,
                Text = d.Text,
            },
            _ => throw new InvalidOperationException($"Unknown answer subtype: {a.GetType().Name}"),
        };

        // Auto-evaluate Single/Multiple. Other types stay null until graded by the teacher.
        if (a is SingleAnswerInProgress or MultipleAnswersInProgress)
        {
            submitted.Mark = EvaluateAnswer(a, questionsById);
        }

        return submitted;
    }

    private static AnswerSubmitted CreateBlankSubmitted(PublishedQuestion question) => question.Type switch
    {
        QuestionType.SingleAnswer => new SingleAnswerSubmitted
        {
            PublishedQuestionId = question.Id,
            SelectedOptionOrder = null,
        },
        QuestionType.MultipleAnswers => new MultipleAnswersSubmitted
        {
            PublishedQuestionId = question.Id,
            SelectedOptionOrders = new List<int>(),
        },
        QuestionType.OpenAnswer => new TextAnswerSubmitted
        {
            PublishedQuestionId = question.Id,
            Text = null,
        },
        QuestionType.Code => new CodeAnswerSubmitted
        {
            PublishedQuestionId = question.Id,
            Text = null,
        },
        QuestionType.Diagram => new DiagramAnswerSubmitted
        {
            PublishedQuestionId = question.Id,
            Text = null,
        },
        _ => throw new InvalidOperationException($"Unknown question type: {question.Type}"),
    };
}
