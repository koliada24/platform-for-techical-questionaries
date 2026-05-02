using Api.Contracts;
using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services.Published;

public class PublishedTestsService : IPublishedTestsService
{
    private readonly AppDbContext _db;

    public PublishedTestsService(AppDbContext db)
    {
        _db = db;
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
                false))
            .ToList();

        var first = publishedTests[0];

        return new PublishedTestDetailDto(
            testTemplateId,
            first.Name,
            first.Description,
            first.TimeLimitMinutes,
            publishedTests.Max(p => p.QuestionCount),
            publishedTests.Select(p => p.GoogleCourseId).Distinct().Count(),
            publishedTests.Min(p => p.CreatedAt),
            closesAt,
            submitted);
    }
}
