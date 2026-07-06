using DepiLms.Models;

namespace DepiLms.ViewModels;

public class QuizQuestionReviewItem
{
    public QuizQuestion Question { get; set; } = default!;
    public QuizAnswer? Answer { get; set; }
    public QuizOption? SelectedOption { get; set; }
    public QuizOption? CorrectOption { get; set; }
    public bool IsCorrect { get; set; }
}

public class QuizReviewViewModel
{
    public QuizAttempt Attempt { get; set; } = default!;
    public decimal MaxScore { get; set; }
    public IReadOnlyList<QuizQuestionReviewItem> Questions { get; set; } = [];
    public bool IsOwner { get; set; }
}

public class QuizTakeViewModel
{
    public int QuizId { get; set; }
    public Quiz Quiz { get; set; } = default!;
    public Dictionary<int, int> SelectedOptions { get; set; } = [];
}

public class QuizResultViewModel
{
    public QuizAttempt Attempt { get; set; } = default!;
    public decimal MaxScore { get; set; }
    public bool CanReview { get; set; }
}

public class QuizAttemptsViewModel
{
    public Quiz Quiz { get; set; } = default!;
    public IReadOnlyCollection<QuizAttempt> Attempts { get; set; } = [];
}

public class QuizHistoryViewModel
{
    public IReadOnlyCollection<QuizAttempt> Attempts { get; set; } = [];
}
