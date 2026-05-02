using Api.Contracts;

namespace Api.Services.Published;

public interface IPublishedTestsService
{
    Task<PublishedTestInfoDto?> GetInfoAsync(Guid id, CancellationToken ct = default);

    Task<List<PublishedTestListItemDto>> ListForTeacherAsync(string teacherId, CancellationToken ct = default);
}
