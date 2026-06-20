using System.ComponentModel.DataAnnotations;
using DepiLms.Models;
using Microsoft.AspNetCore.Http;

namespace DepiLms.ViewModels;

public class AssignmentDetailsViewModel
{
    public Assignment Assignment { get; set; } = default!;
    public AssignmentSubmission? MySubmission { get; set; }
    public IReadOnlyCollection<AssignmentSubmission> Submissions { get; set; } = [];
    public bool CanSubmit { get; set; }
    public bool CanGrade { get; set; }
}

public class AssignmentSubmitViewModel
{
    public int AssignmentId { get; set; }

    [MaxLength(240)]
    public string? FileUrl { get; set; }

    public IFormFile? SubmissionFile { get; set; }
    public string? Notes { get; set; }
}

public class SubmissionGradeViewModel
{
    public int SubmissionId { get; set; }
    public decimal Score { get; set; }
    public string? Feedback { get; set; }
}
