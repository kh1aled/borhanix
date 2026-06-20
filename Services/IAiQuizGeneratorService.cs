using DepiLms.Models;

namespace DepiLms.Services;

public record GeneratedQuizOption(string Text, bool IsCorrect);
public record GeneratedQuizQuestion(string Prompt, IReadOnlyCollection<GeneratedQuizOption> Options);
public record GeneratedQuizResult(string Title, IReadOnlyCollection<GeneratedQuizQuestion> Questions);

public interface IAiQuizGeneratorService
{
    Task<GeneratedQuizResult> GenerateForLessonAsync(Lesson lesson, CancellationToken cancellationToken);
}
