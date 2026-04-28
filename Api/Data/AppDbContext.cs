using Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TestTemplate> TestTemplates => Set<TestTemplate>();
    public DbSet<Question> Questions => Set<Question>();

    public DbSet<Test> Tests => Set<Test>();
    public DbSet<TestQuestion> TestQuestions => Set<TestQuestion>();
    public DbSet<TestAnswerOption> TestAnswerOptions => Set<TestAnswerOption>();

    public DbSet<TestInProcess> TestsInProcess => Set<TestInProcess>();
    public DbSet<TestInProcessAnswer> TestInProcessAnswers => Set<TestInProcessAnswer>();

    public DbSet<TestAnswers> TestAnswers => Set<TestAnswers>();
    public DbSet<TestAnswerSelection> TestAnswerSelections => Set<TestAnswerSelection>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // SQLite cannot ORDER BY / compare DateTimeOffset natively; store as ticks.
        var dtoConverter = new ValueConverter<DateTimeOffset, long>(
            v => v.UtcTicks,
            v => new DateTimeOffset(v, TimeSpan.Zero));
        var dtoNullableConverter = new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? v.Value.UtcTicks : (long?)null,
            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : (DateTimeOffset?)null);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                    property.SetValueConverter(dtoConverter);
                else if (property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(dtoNullableConverter);
            }
        }

        builder.Entity<TestTemplate>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TeacherId);
        });

        builder.Entity<Question>(e =>
        {
            e.Property(x => x.Text).IsRequired().HasMaxLength(2000);
            e.HasOne(x => x.TestTemplate)
                .WithMany(t => t.Questions)
                .HasForeignKey(x => x.TestTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Answers are stored as JSON inside the Questions table.
            e.OwnsMany(x => x.Answers, a =>
            {
                a.ToJson();
                a.Property(p => p.Text).IsRequired().HasMaxLength(1000);
            });
        });

        builder.Entity<Test>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.GoogleCourseId).IsRequired().HasMaxLength(100);
            e.Property(x => x.GoogleCourseName).IsRequired().HasMaxLength(300);
            e.Property(x => x.GoogleCourseWorkId).HasMaxLength(100);
            e.Property(x => x.GoogleCourseWorkLink).HasMaxLength(500);
            e.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TeacherId);
            e.HasIndex(x => x.GoogleCourseId);
        });

        builder.Entity<TestQuestion>(e =>
        {
            e.Property(x => x.Text).IsRequired().HasMaxLength(2000);
            e.HasOne(x => x.Test)
                .WithMany(t => t.Questions)
                .HasForeignKey(x => x.TestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TestAnswerOption>(e =>
        {
            e.Property(x => x.Text).IsRequired().HasMaxLength(1000);
            e.HasOne(x => x.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(x => x.TestQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TestInProcess>(e =>
        {
            e.HasOne(x => x.Test)
                .WithMany()
                .HasForeignKey(x => x.TestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            // One in-progress attempt per (test, student).
            e.HasIndex(x => new { x.TestId, x.StudentId }).IsUnique();
        });

        builder.Entity<TestInProcessAnswer>(e =>
        {
            e.HasOne(x => x.TestInProcess)
                .WithMany(p => p.Selections)
                .HasForeignKey(x => x.TestInProcessId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TestInProcessId, x.TestQuestionId, x.TestAnswerOptionId })
                .IsUnique();
        });

        builder.Entity<TestAnswers>(e =>
        {
            e.HasOne(x => x.Test)
                .WithMany()
                .HasForeignKey(x => x.TestId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TestId, x.StudentId }).IsUnique();
        });

        builder.Entity<TestAnswerSelection>(e =>
        {
            e.HasOne(x => x.TestAnswers)
                .WithMany(a => a.Selections)
                .HasForeignKey(x => x.TestAnswersId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
