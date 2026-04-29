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
    public DbSet<QuestionTemplate> QuestionTemplates => Set<QuestionTemplate>();
    public DbSet<PublishedTest> PublishedTests => Set<PublishedTest>();
    public DbSet<PublishedQuestion> PublishedQuestions => Set<PublishedQuestion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var dtoConverter = new ValueConverter<DateTimeOffset, long>(
            v => v.UtcTicks,
            v => new DateTimeOffset(v, TimeSpan.Zero));
        var dtoNullableConverter = new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? v.Value.UtcTicks : null,
            v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);

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
            e.ToTable("TEMPLATE_Tests");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TeacherId);
        });

        builder.Entity<QuestionTemplate>(e =>
        {
            e.ToTable("TEMPLATE_Questions");
            e.Property(x => x.Text).IsRequired().HasMaxLength(2000);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            e.HasOne(x => x.TestTemplate)
                .WithMany(t => t.Questions)
                .HasForeignKey(x => x.TestTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            e.OwnsMany(x => x.Answers, a =>
            {
                a.ToJson();
                a.Property(p => p.Text).IsRequired().HasMaxLength(1000);
            });
        });

        builder.Entity<PublishedTest>(e =>
        {
            e.ToTable("PUBLISHED_Tests");
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.GoogleCourseId).IsRequired().HasMaxLength(100);
            e.Property(x => x.GoogleCourseName).IsRequired().HasMaxLength(500);
            e.Property(x => x.GoogleCourseWorkId).HasMaxLength(100);
            e.Property(x => x.GoogleCourseWorkLink).HasMaxLength(1000);
            e.HasOne(x => x.Teacher)
                .WithMany()
                .HasForeignKey(x => x.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.TeacherId);
            e.HasIndex(x => x.TestTemplateId);
        });

        builder.Entity<PublishedQuestion>(e =>
        {
            e.ToTable("PUBLISHED_Questions");
            e.Property(x => x.Text).IsRequired().HasMaxLength(2000);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
            e.HasOne(x => x.PublishedTest)
                .WithMany(t => t.Questions)
                .HasForeignKey(x => x.PublishedTestId)
                .OnDelete(DeleteBehavior.Cascade);

            e.OwnsMany(x => x.Answers, a =>
            {
                a.ToJson();
                a.Property(p => p.Text).IsRequired().HasMaxLength(1000);
            });
        });
    }
}
