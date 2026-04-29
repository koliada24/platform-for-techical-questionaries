namespace Api.Contracts;

public record AttemptInProgressDto(
    System.Guid Id,
    System.Guid PublishedTestId,
    System.DateTimeOffset StartedAt
);
