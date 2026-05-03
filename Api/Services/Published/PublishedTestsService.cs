using Api.Contracts;
using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.Published;

public class PublishedTestsService : IPublishedTestsService
{
    private readonly AppDbContext _db;
    private readonly GoogleClassroomClient _classroom;
    private readonly ITeacherProvider _teacherProvider;

    public PublishedTestsService(
        AppDbContext db,
        GoogleClassroomClient classroom,
        ITeacherProvider teacherProvider)
    {
        _db = db;
        _classroom = classroom;
        _teacherProvider = teacherProvider;
    }

    public async Task<PublishedTestInfoDto?> GetInfoAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.PublishedTests
            .Where(t => t.Id == id)
            .Select(t => new PublishedTestInfoDto(
                t.Id,
                t.Name,
                t.Description,
                t.TimeLimitMinutes,
                t.Questions.Count,
                t.ClosesAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<PublishedTestListItemDto>> ListForTeacherAsync(string teacherId, CancellationToken ct = default)
    {
        // Each Publish action creates one PublishedTest row per Google Classroom course.
        // Group rows from the same publication batch by (TestTemplateId, ClosesAt) for this teacher.
        var rows = await _db.PublishedTests
            .Where(t => t.TeacherId == teacherId)
            .Select(t => new
            {
                t.TestTemplateId,
                t.Name,
                t.Description,
                t.TimeLimitMinutes,
                t.ClosesAt,
                t.CreatedAt,
                t.GoogleCourseId,
                QuestionCount = t.Questions.Count,
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => new { r.TestTemplateId, r.ClosesAt })
            .Select(g => new PublishedTestListItemDto(
                g.Key.TestTemplateId,
                g.First().Name,
                g.First().Description,
                g.First().TimeLimitMinutes,
                g.Max(r => r.QuestionCount),
                g.Select(r => r.GoogleCourseId).Distinct().Count(),
                g.Min(r => r.CreatedAt),
                g.Key.ClosesAt))
            .OrderByDescending(x => x.OpenedAt)
            .ToList();
    }

    public async Task<PublishedTestDetailDto?> GetDetailForTeacherAsync(
        string teacherId,
        Guid testTemplateId,
        DateTimeOffset closesAt,
        CancellationToken ct = default)
    {
        var publishedTests = await _db.PublishedTests
            .Where(t => t.TeacherId == teacherId
                && t.TestTemplateId == testTemplateId
                && t.ClosesAt == closesAt)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.TimeLimitMinutes,
                t.CreatedAt,
                t.ClosesAt,
                t.GoogleCourseId,
                QuestionCount = t.Questions.Count,
                MaxMark = t.Questions.Sum(q => q.Mark),
            })
            .ToListAsync(ct);

        if (publishedTests.Count == 0)
        {
            return null;
        }

        var publishedTestIds = publishedTests.Select(p => p.Id).ToList();

        var submittedRows = await _db.AttemptsSubmitted
            .Where(a => publishedTestIds.Contains(a.PublishedTestId))
            .Join(_db.Users,
                a => a.StudentId,
                u => u.Id,
                (a, u) => new
                {
                    a.Id,
                    a.StudentId,
                    u.FullName,
                    u.Email,
                    u.PictureUrl,
                    a.StartedAt,
                    a.SubmittedAt,
                    a.DurationSeconds,
                    a.EvaluatedMark,
                    a.MarkSent,
                })
            .ToListAsync(ct);

        var submitted = submittedRows
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new SubmittedAttemptSummaryDto(
                s.Id,
                s.StudentId,
                s.FullName,
                s.Email,
                s.PictureUrl,
                s.StartedAt,
                s.SubmittedAt,
                s.DurationSeconds,
                s.EvaluatedMark,
                s.MarkSent))
            .ToList();

        var first = publishedTests[0];

        return new PublishedTestDetailDto(
            testTemplateId,
            first.Name,
            first.Description,
            first.TimeLimitMinutes,
            publishedTests.Max(p => p.QuestionCount),
            publishedTests.Max(p => p.MaxMark),
            publishedTests.Select(p => p.GoogleCourseId).Distinct().Count(),
            publishedTests.Min(p => p.CreatedAt),
            closesAt,
            submitted);
    }

    public async Task<AttemptDetailForTeacherDto?> GetAttemptDetailForTeacherAsync(
        string teacherId,
        Guid attemptId,
        CancellationToken ct = default)
    {
        var attempt = await _db.AttemptsSubmitted
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);
        if (attempt is null) return null;

        var publishedTest = await _db.PublishedTests
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == attempt.PublishedTestId && t.TeacherId == teacherId, ct);
        if (publishedTest is null) return null;

        var student = await _db.Users.FirstOrDefaultAsync(u => u.Id == attempt.StudentId, ct);

        return BuildDetailDto(attempt, publishedTest, student);
    }

    public async Task<SetManualMarksResult> SetManualMarksAsync(
        string teacherId,
        Guid attemptId,
        List<SetManualMarkInput> marks,
        CancellationToken ct = default)
    {
        var attempt = await _db.AttemptsSubmitted
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);
        if (attempt is null) return new SetManualMarksResult.AttemptNotFound();

        var publishedTest = await _db.PublishedTests
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == attempt.PublishedTestId && t.TeacherId == teacherId, ct);
        if (publishedTest is null) return new SetManualMarksResult.AttemptNotFound();

        var questionsById = publishedTest.Questions.ToDictionary(q => q.Id);
        var answersByQuestion = attempt.Answers.ToDictionary(a => a.PublishedQuestionId);

        foreach (var input in marks)
        {
            if (!answersByQuestion.TryGetValue(input.PublishedQuestionId, out var answer))
            {
                continue;
            }
            if (answer is SingleAnswerSubmitted or MultipleAnswersSubmitted)
            {
                // Auto-evaluated; ignore manual override.
                continue;
            }
            if (!questionsById.TryGetValue(answer.PublishedQuestionId, out var question))
            {
                continue;
            }
            if (input.Mark is null)
            {
                answer.Mark = null;
                continue;
            }
            if (input.Mark < 0 || input.Mark > question.Mark)
            {
                return new SetManualMarksResult.InvalidMark(
                    $"Mark for question {question.Order + 1} must be between 0 and {question.Mark}.");
            }
            answer.Mark = input.Mark;
        }

        attempt.EvaluatedMark = attempt.Answers.Sum(a => a.Mark ?? 0);
        await _db.SaveChangesAsync(ct);

        var student = await _db.Users.FirstOrDefaultAsync(u => u.Id == attempt.StudentId, ct);
        return new SetManualMarksResult.Success(BuildDetailDto(attempt, publishedTest, student));
    }

    public async Task<SendMarkResult> SendMarkToClassroomAsync(
        string teacherId,
        Guid attemptId,
        CancellationToken ct = default)
    {
        var attempt = await _db.AttemptsSubmitted
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);
        if (attempt is null) return new SendMarkResult.AttemptNotFound();

        var publishedTest = await _db.PublishedTests
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == attempt.PublishedTestId && t.TeacherId == teacherId, ct);
        if (publishedTest is null) return new SendMarkResult.AttemptNotFound();

        if (attempt.Answers.Any(a => a.Mark is null))
        {
            return new SendMarkResult.NotFullyEvaluated();
        }

        if (string.IsNullOrEmpty(publishedTest.GoogleCourseId)
            || string.IsNullOrEmpty(publishedTest.GoogleCourseWorkId))
        {
            return new SendMarkResult.ClassroomFailure("This test is not linked to a Classroom assignment.");
        }

        var teacher = await _teacherProvider.GetTeacherAsync(teacherId, ct);
        if (teacher is null) return new SendMarkResult.ClassroomFailure("Teacher account not found.");

        var student = await _db.Users.FirstOrDefaultAsync(u => u.Id == attempt.StudentId, ct);
        if (student is null || string.IsNullOrEmpty(student.GoogleId))
        {
            return new SendMarkResult.ClassroomFailure("Student is not linked to a Google account.");
        }

        var totalMark = attempt.Answers.Sum(a => a.Mark ?? 0);

        try
        {
            await _classroom.SendStudentSubmissionGradeAsync(
                teacher,
                publishedTest.GoogleCourseId,
                publishedTest.GoogleCourseWorkId!,
                student.GoogleId!,
                totalMark,
                ct);
        }
        catch (Exception ex)
        {
            return new SendMarkResult.ClassroomFailure(ex.Message);
        }

        attempt.EvaluatedMark = totalMark;
        attempt.MarkSent = true;
        attempt.MarkSentAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var maxMark = publishedTest.Questions.Sum(q => q.Mark);
        return new SendMarkResult.Success(totalMark, maxMark);
    }

    private static AttemptDetailForTeacherDto BuildDetailDto(
        AttemptSubmitted attempt,
        PublishedTest publishedTest,
        ApplicationUser? student)
    {
        var questionsByIdLocal = publishedTest.Questions.ToDictionary(q => q.Id);
        var answersByQuestion = attempt.Answers.ToDictionary(a => a.PublishedQuestionId);
        var maxMark = publishedTest.Questions.Sum(q => q.Mark);

        var questions = publishedTest.Questions
            .OrderBy(q => q.Order)
            .Select(q =>
            {
                answersByQuestion.TryGetValue(q.Id, out var answer);
                int? selectedSingle = null;
                List<int>? selectedMultiple = null;
                string? answerText = null;

                switch (answer)
                {
                    case SingleAnswerSubmitted s:
                        selectedSingle = s.SelectedOptionOrder;
                        break;
                    case MultipleAnswersSubmitted m:
                        selectedMultiple = m.SelectedOptionOrders.ToList();
                        break;
                    case TextAnswerSubmitted t:
                        answerText = t.Text;
                        break;
                    case CodeAnswerSubmitted c:
                        answerText = c.Code;
                        break;
                    case DiagramAnswerSubmitted d:
                        answerText = d.Diagram;
                        break;
                }

                var isAuto = q.Type is QuestionType.SingleAnswer or QuestionType.MultipleAnswers;

                return new AttemptQuestionForTeacherDto(
                    q.Id,
                    q.Text,
                    q.Order,
                    q.Mark,
                    q.Type,
                    q.CodeLanguage,
                    q.Answers.OrderBy(a => a.Order)
                        .Select(a => new AttemptAnswerOptionDto(a.Order, a.Text, a.IsCorrect))
                        .ToList(),
                    selectedSingle,
                    selectedMultiple,
                    answerText,
                    answer?.Mark,
                    isAuto);
            })
            .ToList();

        var totalMark = attempt.Answers.Sum(a => a.Mark ?? 0);
        var fullyEvaluated = attempt.Answers.Count > 0 && attempt.Answers.All(a => a.Mark is not null);

        return new AttemptDetailForTeacherDto(
            attempt.Id,
            publishedTest.TestTemplateId,
            publishedTest.ClosesAt,
            publishedTest.Name,
            attempt.StudentId,
            student?.FullName,
            student?.Email,
            student?.PictureUrl,
            attempt.StartedAt,
            attempt.SubmittedAt,
            attempt.DurationSeconds,
            maxMark,
            totalMark,
            fullyEvaluated,
            attempt.MarkSent,
            questions);
    }
}
