using Api.Models;

namespace Api.Contracts;

public record AttemptInProgressDto(
    Guid Id,
    Guid PublishedTestId,
    DateTimeOffset StartedAt
);

public record AnswerOptionForStudentDto(int Order, string Text);

public record SavedAnswerDto(
    QuestionType Type,
    int? SelectedOptionOrder,
    List<int>? SelectedOptionOrders,
    string? Text
);

public record AttemptQuestionForStudentDto(
    Guid Id,
    string Text,
    int Order,
    QuestionType Type,
    List<AnswerOptionForStudentDto> Options,
    SavedAnswerDto? SavedAnswer
);

public record AttemptForStudentDto(
    Guid Id,
    Guid PublishedTestId,
    string Name,
    string? Description,
    int? TimeLimitMinutes,
    DateTimeOffset StartedAt,
    DateTimeOffset ClosesAt,
    List<AttemptQuestionForStudentDto> Questions
);

public record SaveSingleAnswerInput(int? SelectedOptionOrder);
public record SaveMultipleAnswersInput(System.Collections.Generic.List<int> SelectedOptionOrders);
public record SaveTextAnswerInput(string? Text);
public record SaveCodeAnswerInput(string? Text);
public record SaveDiagramAnswerInput(string? Text);
