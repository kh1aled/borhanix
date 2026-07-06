using System.Text.Json;
using System.Text.RegularExpressions;
using DepiLms.Models;

namespace DepiLms.Services;

public partial class AiQuizGeneratorService(IOpenRouterAiService aiService) : IAiQuizGeneratorService
{
    public async Task<GeneratedQuizResult> GenerateForLessonAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        var prompt = $$"""
        Generate a short multiple-choice quiz for this LMS lesson.
        Return JSON only with this shape:
        {
          "title": "Quiz title",
          "questions": [
            {
              "prompt": "Question text",
              "options": [
                { "text": "Option", "isCorrect": true },
                { "text": "Option", "isCorrect": false }
              ]
            }
          ]
        }
        Requirements:
        - exactly 5 questions
        - exactly 4 options per question
        - one correct option per question
        - practical for DEPI .NET learners

        Lesson title: {{lesson.Title}}
        Lesson content: {{lesson.Content}}
        Video URL: {{lesson.VideoUrl}}
        """;

        var response = await aiService.AskAsync([], prompt, "You generate strict JSON quiz content for an LMS.", cancellationToken);
        var parsed = TryParse(response.Content);
        return parsed ?? BuildFallback(lesson);
    }

    private static GeneratedQuizResult? TryParse(string content)
    {
        var json = StripCodeFence(content).Trim();
        try
        {
            var payload = JsonSerializer.Deserialize<GeneratedQuizPayload>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload?.Questions is null || payload.Questions.Count == 0)
            {
                return null;
            }

            return new GeneratedQuizResult(
                string.IsNullOrWhiteSpace(payload.Title) ? "AI lesson quiz" : payload.Title,
                payload.Questions
                    .Where(q => !string.IsNullOrWhiteSpace(q.Prompt) && q.Options.Count >= 2)
                    .Take(8)
                    .Select(q => new GeneratedQuizQuestion(
                        q.Prompt,
                        NormalizeOptions(q.Options)))
                    .ToList());
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyCollection<GeneratedQuizOption> NormalizeOptions(List<GeneratedOptionPayload> options)
    {
        var normalized = options
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Take(4)
            .Select(x => new GeneratedQuizOption(x.Text, x.IsCorrect))
            .ToList();

        if (!normalized.Any(x => x.IsCorrect) && normalized.Count > 0)
        {
            normalized[0] = normalized[0] with { IsCorrect = true };
        }

        return normalized;
    }

    private static GeneratedQuizResult BuildFallback(Lesson lesson)
        => new(
            $"{lesson.Title} practice quiz",
            [
                BuildQuestion($"What is the main goal of the lesson '{lesson.Title}'?", "Understand and apply the core concept"),
                BuildQuestion("What should you do after watching the lesson video?", "Practice the concept in code"),
                BuildQuestion("Which habit improves your LMS project quality?", "Testing each feature after implementation"),
                BuildQuestion("What should a student submit for proof of work?", "A clear solution with notes or an uploaded file"),
                BuildQuestion("How should instructors use quiz results?", "To identify topics that need review")
            ]);

    private static GeneratedQuizQuestion BuildQuestion(string prompt, string correct)
        => new(prompt,
        [
            new GeneratedQuizOption(correct, true),
            new GeneratedQuizOption("Skip the practical work", false),
            new GeneratedQuizOption("Ignore feedback", false),
            new GeneratedQuizOption("Delete the lesson content", false)
        ]);

    private static string StripCodeFence(string content)
    {
        var match = JsonFenceRegex().Match(content);
        return match.Success ? match.Groups["json"].Value : content;
    }

    [GeneratedRegex("```(?:json)?\\s*(?<json>[\\s\\S]*?)\\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex JsonFenceRegex();

    private sealed class GeneratedQuizPayload
    {
        public string? Title { get; set; }
        public List<GeneratedQuestionPayload> Questions { get; set; } = [];
    }

    private sealed class GeneratedQuestionPayload
    {
        public string Prompt { get; set; } = string.Empty;
        public List<GeneratedOptionPayload> Options { get; set; } = [];
    }

    private sealed class GeneratedOptionPayload
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
