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
}
