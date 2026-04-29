using Api.Contracts;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.InProgress;

public class TestsInProgressService : ITestsInProgressService
{
    private readonly AppDbContext _db;

    public TestsInProgressService(AppDbContext db)
    {
        _db = db;
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
}
