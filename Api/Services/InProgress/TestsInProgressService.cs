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
                    .ToList()))
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
}
