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
    public DbSet<TestTemplateAssignment> TestTemplateAssignments => Set<TestTemplateAssignment>();

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

        builder.Entity<TestTemplateAssignment>(e =>
        {
            e.Property(x => x.GoogleCourseId).IsRequired().HasMaxLength(100);
            e.Property(x => x.GoogleCourseName).IsRequired().HasMaxLength(300);
            e.HasOne(x => x.TestTemplate)
                .WithMany(t => t.Assignments)
                .HasForeignKey(x => x.TestTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TestTemplateId, x.GoogleCourseId }).IsUnique();
        });
    }
}
