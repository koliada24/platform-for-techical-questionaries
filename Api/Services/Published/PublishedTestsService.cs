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
}
