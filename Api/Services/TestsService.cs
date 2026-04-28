using Api.Contracts;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class TestsService : ITestsService
{
    private readonly AppDbContext _db;

    public TestsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TestForStudentDto?> GetForStudentAsync(Guid testId, CancellationToken ct = default)
    {
        var test = await _db.Tests
            .Include(t => t.Questions.OrderBy(q => q.Order))
                .ThenInclude(q => q.Options.OrderBy(o => o.Order))
            .FirstOrDefaultAsync(t => t.Id == testId, ct);

        if (test is null)
        {
            return null;
        }

        return new TestForStudentDto(
            test.Id, test.Name, test.Description, test.TimeLimitMinutes, test.ClosesAt,
            test.Questions
                .OrderBy(q => q.Order)
                .Select(q => new TestQuestionForStudentDto(
                    q.Id, q.Text, q.Order,
                    q.Options
                        .OrderBy(o => o.Order)
                        .Select(o => new TestAnswerOptionForStudentDto(o.Id, o.Text, o.Order))
                        .ToList()))
                .ToList());
    }

    public async Task<StartTestResult> StartAsync(string studentId, Guid testId, CancellationToken ct = default)
    {
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.Id == testId, ct);
        if (test is null) return new StartTestResult.TestNotFound();

        if (test.ClosesAt <= DateTimeOffset.UtcNow)
            return new StartTestResult.TestClosed();

        var alreadySubmitted = await _db.TestAnswers
            .AnyAsync(a => a.TestId == testId && a.StudentId == studentId, ct);
        if (alreadySubmitted) return new StartTestResult.AlreadySubmitted();

        var existing = await _db.TestsInProcess
            .Include(p => p.Selections)
            .FirstOrDefaultAsync(p => p.TestId == testId && p.StudentId == studentId, ct);

        if (existing is not null)
        {
            return new StartTestResult.Success(MapInProcess(existing), AlreadyExisted: true);
        }

        var attempt = new TestInProcess
        {
            TestId = testId,
            StudentId = studentId,
        };
        _db.TestsInProcess.Add(attempt);
        await _db.SaveChangesAsync(ct);

        return new StartTestResult.Success(MapInProcess(attempt), AlreadyExisted: false);
    }

    public async Task<TestInProcessDto?> GetInProcessAsync(string studentId, Guid inProcessId, CancellationToken ct = default)
    {
        var attempt = await _db.TestsInProcess
            .Include(p => p.Selections)
            .FirstOrDefaultAsync(p => p.Id == inProcessId && p.StudentId == studentId, ct);

        return attempt is null ? null : MapInProcess(attempt);
    }

    public async Task<TestInProcessDto?> SaveSelectionsAsync(string studentId, Guid inProcessId, List<TestSelectionDto> selections, CancellationToken ct = default)
    {
        var attempt = await _db.TestsInProcess
            .Include(p => p.Selections)
            .FirstOrDefaultAsync(p => p.Id == inProcessId && p.StudentId == studentId, ct);
        
        if (attempt is null)
        {
            return null;   
        }

        var validPairs = await _db.TestAnswerOptions
            .Where(o => o.Question!.TestId == attempt.TestId)
            .Select(o => new { o.TestQuestionId, OptionId = o.Id })
            .ToListAsync(ct);
        var validSet = validPairs
            .Select(p => (p.TestQuestionId, p.OptionId))
            .ToHashSet();

        var deduped = selections
            .Where(s => validSet.Contains((s.TestQuestionId, s.TestAnswerOptionId)))
            .GroupBy(s => (s.TestQuestionId, s.TestAnswerOptionId))
            .Select(g => g.First())
            .ToList();

        _db.TestInProcessAnswers.RemoveRange(attempt.Selections);
        attempt.Selections.Clear();
        foreach (var s in deduped)
        {
            attempt.Selections.Add(new TestInProcessAnswer
            {
                TestInProcessId = attempt.Id,
                TestQuestionId = s.TestQuestionId,
                TestAnswerOptionId = s.TestAnswerOptionId,
            });
        }
        attempt.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return MapInProcess(attempt);
    }

    public async Task<SubmitTestResult> SubmitAsync(string studentId, Guid inProcessId, CancellationToken ct = default)
    {
        var attempt = await _db.TestsInProcess
            .Include(p => p.Selections)
            .FirstOrDefaultAsync(p => p.Id == inProcessId && p.StudentId == studentId, ct);
        if (attempt is null) return new SubmitTestResult.InProcessNotFound();

        var questions = await _db.TestQuestions
            .Where(q => q.TestId == attempt.TestId)
            .Where(q => q.Type == QuestionType.SingleAnswer || q.Type == QuestionType.MultipleAnswers)
            .Select(q => new
            {
                q.Id,
                CorrectIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).ToList()
            })
            .ToListAsync(ct);

        var selectedByQuestion = attempt.Selections
            .GroupBy(s => s.TestQuestionId)
            .ToDictionary(g => g.Key, g => g.Select(s => s.TestAnswerOptionId).ToHashSet());

        int correct = 0;
        foreach (var q in questions)
        {
            selectedByQuestion.TryGetValue(q.Id, out var picked);
            picked ??= new HashSet<Guid>();
            var expected = q.CorrectIds.ToHashSet();
            if (picked.SetEquals(expected)) correct++;
        }

        var answers = new TestAnswers
        {
            TestId = attempt.TestId,
            StudentId = studentId,
            StartedAt = attempt.StartedAt,
            SubmittedAt = DateTimeOffset.UtcNow,
            CorrectCount = correct,
            QuestionCount = questions.Count,
            Selections = attempt.Selections
                .Select(s => new TestAnswerSelection
                {
                    TestQuestionId = s.TestQuestionId,
                    TestAnswerOptionId = s.TestAnswerOptionId,
                })
                .ToList(),
        };

        _db.TestAnswers.Add(answers);
        _db.TestsInProcess.Remove(attempt);
        await _db.SaveChangesAsync(ct);

        var dto = new TestAnswersDto(
            answers.Id, answers.TestId, answers.StartedAt, answers.SubmittedAt,
            answers.CorrectCount, answers.QuestionCount,
            answers.Selections
                .Select(s => new TestSelectionDto(s.TestQuestionId, s.TestAnswerOptionId))
                .ToList());

        return new SubmitTestResult.Success(dto);
    }

    private static TestInProcessDto MapInProcess(TestInProcess p) => new(
        p.Id, p.TestId, p.StartedAt, p.UpdatedAt,
        p.Selections
            .Select(s => new TestSelectionDto(s.TestQuestionId, s.TestAnswerOptionId))
            .ToList());
}
