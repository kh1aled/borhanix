using DepiLms.Models;

namespace DepiLms.ViewModels;

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
