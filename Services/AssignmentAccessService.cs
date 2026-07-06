using DepiLms.Data;
using DepiLms.Models;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Services;

public interface IAssignmentAccessService
{
    Task<AssignmentAccess> EnsureAccessAsync(Assignment assignment, string studentId);
    Task RefreshStatusAsync(AssignmentAccess access, AssignmentSubmission? submission);
}

public class AssignmentAccessService(ApplicationDbContext db) : IAssignmentAccessService
{
    public async Task<AssignmentAccess> EnsureAccessAsync(Assignment assignment, string studentId)
    {
        var access = await db.AssignmentAccessRecords
            .FirstOrDefaultAsync(x => x.AssignmentId == assignment.Id && x.StudentId == studentId);

        if (access is not null)
        {
            return access;
        }

        var now = DateTimeOffset.UtcNow;
        access = new AssignmentAccess
        {
            AssignmentId = assignment.Id,
            StudentId = studentId,
            FirstAccessedAt = now,
            PersonalDeadlineAt = now.AddHours(assignment.DeadlineHours),
            Status = AssignmentStudentStatus.Active
        };

        db.AssignmentAccessRecords.Add(access);
        await db.SaveChangesAsync();
        return access;
    }

    public async Task RefreshStatusAsync(AssignmentAccess access, AssignmentSubmission? submission)
    {
        if (submission is not null)
        {
            if (access.Status != AssignmentStudentStatus.Submitted)
            {
                access.Status = AssignmentStudentStatus.Submitted;
                await db.SaveChangesAsync();
            }

            return;
        }

        if (access.Status == AssignmentStudentStatus.Active && DateTimeOffset.UtcNow > access.PersonalDeadlineAt)
        {
            access.Status = AssignmentStudentStatus.Missing;
            await db.SaveChangesAsync();
        }
    }
}
