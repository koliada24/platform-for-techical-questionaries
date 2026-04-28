using Api.Contracts;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class TestsService : ITestsService
{
    private readonly AppDbContext _db;
    private readonly GoogleClassroomClient _classroom;
    private readonly ITeacherProvider _teacherProvider;

    public TestsService(AppDbContext db, GoogleClassroomClient classroom, ITeacherProvider teacherProvider)
    {
        _db = db;
        _classroom = classroom;
        _teacherProvider = teacherProvider;
    }

    public Task<List<TestSummaryDto>> ListAsync(string teacherId, CancellationToken ct = default)
    {
        return _db.Tests
            .Where(t => t.TeacherId == teacherId)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TestSummaryDto(
                t.Id, t.Name, t.Description, t.TimeLimitMinutes,
                t.Questions.Count, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<TestDto?> GetAsync(string teacherId, Guid id, CancellationToken ct = default)
    {
        var test = await _db.Tests
            .Include(t => t.Questions.OrderBy(q => q.Order))
            .FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacherId, ct);

        return test is null ? null : MapToDto(test);
    }

    public async Task<TestDto> CreateAsync(string teacherId, TestInput input, CancellationToken ct = default)
    {
        var test = new Test
        {
            TeacherId = teacherId,
            Name = input.Name.Trim(),
            Description = NormalizeDescription(input.Description),
            TimeLimitMinutes = input.TimeLimitMinutes,
        };

        foreach (var qIn in input.Questions)
        {
            test.Questions.Add(ToQuestion(qIn, test.Id));
        }

        _db.Tests.Add(test);
        await _db.SaveChangesAsync(ct);

        return MapToDto(test);
    }

    public async Task<TestDto?> UpdateAsync(string teacherId, Guid id, TestInput input, CancellationToken ct = default)
    {
        var test = await _db.Tests
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacherId, ct);
        if (test is null) return null;

        test.Name = input.Name.Trim();
        test.Description = NormalizeDescription(input.Description);
        test.TimeLimitMinutes = input.TimeLimitMinutes;
        test.UpdatedAt = DateTimeOffset.UtcNow;

        var questionsToStay = input.Questions.Where(q => q.Id != null).Select(x => x.Id);
        test.Questions.RemoveAll(q => !questionsToStay.Contains(q.Id));

        var questionsToAdd = input.Questions
            .Where(q => q.Id == null)
            .Select(q => ToQuestion(q, test.Id)).ToArray();
        _db.Questions.AddRange(questionsToAdd);

        await _db.SaveChangesAsync(ct);

        return MapToDto(test);
    }

    public async Task<bool> DeleteAsync(string teacherId, Guid id, CancellationToken ct = default)
    {
        var test = await _db.Tests.FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacherId, ct);
        
        if (test is null)
        {
            return false;
        }

        _db.Tests.Remove(test);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<PublishResult> PublishAsync(string teacherId, Guid id, PublishTestRequest request, CancellationToken ct = default)
    {
        var test = await _db.Tests
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacherId, ct);

        if (test is null)
        {
            return new PublishResult.TestNotFound();
        }

        var teacher = await _teacherProvider.GetTeacherAsync(teacherId, ct);

        if (teacher is null)
        {
            return new PublishResult.ClassroomFailure("Teacher account not found.");
        }

        List<GoogleClassroomClient.CourseInfo> courses;
        try
        {
            courses = await _classroom.GetTeacherCoursesAsync(teacher, ct);
        }
        catch (Exception ex)
        {
            return new PublishResult.ClassroomFailure(ex.Message);
        }

        var byId = courses.ToDictionary(c => c.Id, c => c);
        var unknown = request.CourseIds.Where(cid => !byId.ContainsKey(cid)).ToList();

        if (unknown.Count > 0)
        {
            return new PublishResult.UnknownCourses(unknown);
        }

        var existing = test.Assignments.ToDictionary(a => a.GoogleCourseId, a => a);
        foreach (var cid in request.CourseIds.Distinct())
        {
            if (existing.TryGetValue(cid, out var current))
            {
                current.ClosesAt = request.ClosesAt;
                continue;
            }
            var info = byId[cid];
            test.Assignments.Add(new TestAssignment
            {
                TestId = test.Id,
                GoogleCourseId = cid,
                GoogleCourseName = info.Name,
                ClosesAt = request.ClosesAt
            });
        }
        test.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var dto = test.Assignments
            .Select(a => new TestAssignmentDto(a.Id, a.GoogleCourseId, a.GoogleCourseName, a.ClosesAt, a.CreatedAt))
            .ToList();
        return new PublishResult.Success(dto);
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static Question ToQuestion(QuestionInput qIn, Guid testId) => new()
    {
        Id = Guid.NewGuid(),
        TestId = testId,
        Text = qIn.Text.Trim(),
        Order = qIn.Order,
        Answers = MapAnswers(qIn.Answers)
    };

    private static List<Answer> MapAnswers(List<AnswerInput> answers)
    {
        var result = new List<Answer>(answers.Count);
        for (int ai = 0; ai < answers.Count; ai++)
        {
            var a = answers[ai];
            result.Add(new Answer
            {
                Text = a.Text.Trim(),
                IsCorrect = a.IsCorrect,
                Order = ai
            });
        }
        return result;
    }

    private static TestDto MapToDto(Test t) => new(
        t.Id, t.Name, t.Description, t.TimeLimitMinutes, t.CreatedAt, t.UpdatedAt,
        t.Questions.OrderBy(q => q.Order).Select(q => new QuestionDto(
            q.Id, q.Text, q.Order,
            q.Answers.OrderBy(a => a.Order)
                .Select(a => new AnswerDto(a.Text, a.IsCorrect, a.Order))
                .ToList()
        )).ToList()
    );
}
