using Api.Contracts;
using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class TestTemplatesService : ITestTemplatesService
{
    private readonly AppDbContext _db;
    private readonly GoogleClassroomClient _classroom;
    private readonly ITeacherProvider _teacherProvider;
    private readonly IConfiguration _config;
    private readonly ILogger<TestTemplatesService> _logger;

    public TestTemplatesService(
        AppDbContext db,
        GoogleClassroomClient classroom,
        ITeacherProvider teacherProvider,
        IConfiguration config,
        ILogger<TestTemplatesService> logger)
    {
        _db = db;
        _classroom = classroom;
        _teacherProvider = teacherProvider;
        _config = config;
        _logger = logger;
    }

    public Task<List<TestTemplateSummaryDto>> ListAsync(string teacherId, CancellationToken ct = default)
    {
        return _db.TestTemplates
            .Where(t => t.TeacherId == teacherId)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TestTemplateSummaryDto(
                t.Id, t.Name, t.Description, t.TimeLimitMinutes,
                t.Questions.Count, t.CreatedAt, t.UpdatedAt))
            .ToListAsync(ct);
    }

    public async Task<TestTemplateDto?> GetAsync(string teacherId, Guid id, CancellationToken ct = default)
    {
        var template = await _db.TestTemplates
            .Include(t => t.Questions.OrderBy(q => q.Order))
            .FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacherId, ct);

        return template is null ? null : MapToDto(template);
    }

    public async Task<TestTemplateDto> CreateAsync(string teacherId, TestTemplateInput input, CancellationToken ct = default)
    {
        var template = new TestTemplate
        {
            TeacherId = teacherId,
            Name = input.Name.Trim(),
            Description = NormalizeDescription(input.Description),
            TimeLimitMinutes = input.TimeLimitMinutes,
        };

        foreach (var qIn in input.Questions)
        {
            template.Questions.Add(ToQuestion(qIn, template.Id));
        }

        _db.TestTemplates.Add(template);
        await _db.SaveChangesAsync(ct);

        return MapToDto(template);
    }

    public async Task<TestTemplateDto?> UpdateAsync(string teacherId, Guid id, TestTemplateInput input, CancellationToken ct = default)
    {
        var template = await _db.TestTemplates
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacherId, ct);
        if (template is null) return null;

        template.Name = input.Name.Trim();
        template.Description = NormalizeDescription(input.Description);
        template.TimeLimitMinutes = input.TimeLimitMinutes;
        template.UpdatedAt = DateTimeOffset.UtcNow;

        var questionsToStay = input.Questions.Where(q => q.Id != null);
        var questionsToStayIds = questionsToStay.Select(x => x.Id);

        foreach (var question in questionsToStay)
        {
            var questionToUpdate = template.Questions.First(q => q.Id == question.Id);
            UpdateQuestion(questionToUpdate, question);
        }
        template.Questions.RemoveAll(q => !questionsToStayIds.Contains(q.Id));

        var questionsToAdd = input.Questions
            .Where(q => q.Id == null)
            .Select(q => ToQuestion(q, template.Id)).ToArray();
        _db.Questions.AddRange(questionsToAdd);

        await _db.SaveChangesAsync(ct);

        return MapToDto(template);
    }

    private void UpdateQuestion(Question questionToUpdate, QuestionInput questionInput)
    {
        questionToUpdate.Text = questionInput.Text;
        questionToUpdate.Order = questionInput.Order;
        questionToUpdate.Type = questionInput.Type;
        questionToUpdate.Answers = questionInput.Answers.Select(a => a.ToAnswer()).ToList();
    }

    public async Task<bool> DeleteAsync(string teacherId, Guid id, CancellationToken ct = default)
    {
        var template = await _db.TestTemplates.FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacherId, ct);
        
        if (template is null)
        {
            return false;
        }

        _db.TestTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);

        return true;
    }

    public async Task<PublishResult> PublishAsync(string teacherId, Guid id, PublishTestTemplateRequest request, CancellationToken ct = default)
    {
        var template = await _db.TestTemplates
            .Include(t => t.Questions)
            .FirstOrDefaultAsync(t => t.Id == id && t.TeacherId == teacherId, ct);

        if (template is null)
        {
            return new PublishResult.TestTemplateNotFound();
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

        var created = new List<Test>();
        foreach (var cid in request.CourseIds.Distinct())
        {
            var info = byId[cid];
            var test = new Test
            {
                TeacherId = teacherId,
                Name = template.Name,
                Description = template.Description,
                TimeLimitMinutes = template.TimeLimitMinutes,
                GoogleCourseId = cid,
                GoogleCourseName = info.Name,
                ClosesAt = request.ClosesAt,
                Questions = template.Questions
                    .OrderBy(q => q.Order)
                    .Select(q => new TestQuestion
                    {
                        Text = q.Text,
                        Order = q.Order,
                        Type = q.Type,
                        Options = q.Answers
                            .OrderBy(a => a.Order)
                            .Select(a => new TestAnswerOption
                            {
                                Text = a.Text,
                                IsCorrect = a.IsCorrect,
                                Order = a.Order
                            })
                            .ToList()
                    })
                    .ToList()
            };
            _db.Tests.Add(test);
            created.Add(test);
        }

        await _db.SaveChangesAsync(ct);

        var clientBaseUrl = (_config["Client:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        foreach (var test in created)
        {
            var linkUrl = $"{clientBaseUrl}/tests/{test.Id}";
            try
            {
                var work = await _classroom.CreateCourseWorkAsync(
                    teacher,
                    test.GoogleCourseId,
                    title: test.Name,
                    description: BuildAssignmentDescription(test.Description, linkUrl),
                    linkUrl: linkUrl,
                    closesAt: test.ClosesAt,
                    ct: ct);
                test.GoogleCourseWorkId = work.Id;
                test.GoogleCourseWorkLink = work.AlternateLink;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Classroom assignment for test {TestId} in course {CourseId}",
                    test.Id, test.GoogleCourseId);
                return new PublishResult.ClassroomFailure(
                    $"Test saved but Google Classroom assignment could not be created: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(ct);

        var dto = created
            .Select(t => new TestSummaryDto(
                t.Id, t.Name, t.Description, t.TimeLimitMinutes,
                t.GoogleCourseId, t.GoogleCourseName, t.ClosesAt, t.CreatedAt))
            .ToList();
        return new PublishResult.Success(dto);
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    private static string BuildAssignmentDescription(string? testDescription, string linkUrl)
    {
        var body = string.IsNullOrWhiteSpace(testDescription) ? "" : testDescription.Trim() + "\n\n";
        return body + $"Take the test: {linkUrl}";
    }

    private static Question ToQuestion(QuestionInput qIn, Guid testTemplateId) => new()
    {
        Id = Guid.NewGuid(),
        TestTemplateId = testTemplateId,
        Text = qIn.Text.Trim(),
        Order = qIn.Order,
        Type = qIn.Type,
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

    private static TestTemplateDto MapToDto(TestTemplate t) => new(
        t.Id, t.Name, t.Description, t.TimeLimitMinutes, t.CreatedAt, t.UpdatedAt,
        t.Questions.OrderBy(q => q.Order).Select(q => new QuestionDto(
            q.Id, q.Text, q.Order, q.Type,
            q.Answers.OrderBy(a => a.Order)
                .Select(a => new AnswerDto(a.Text, a.IsCorrect, a.Order))
                .ToList()
        )).ToList()
    );
}
