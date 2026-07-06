using DepiLms.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DepiLms.Data;

public class PlatformSeeder(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration)
{
    public async Task SeedAsync()
    {
        await db.Database.EnsureCreatedAsync();
        await EnsureRolesAsync();

        var admin = await EnsureUserAsync(
            configuration["SeedAdmin:Email"] ?? "admin@depi.edu",
            configuration["SeedAdmin:Password"] ?? "Admin@12345",
            configuration["SeedAdmin:FullName"] ?? "DEPI Platform Admin",
            AppRoles.Admin);

        var instructor = await EnsureUserAsync(
            "instructor@depi.edu",
            "Instructor@12345",
            "Nour Hassan",
            AppRoles.Instructor);

        var student = await EnsureUserAsync(
            "student@depi.edu",
            "Student@12345",
            "Omar Ahmed",
            AppRoles.Student);

        await EnsureInstructorProfileAsync(instructor);
        await EnsureStudentProfileAsync(student);

        if (!await db.Courses.AnyAsync())
        {
            var course = new Course
            {
                Code = "DOTNET-LMS-01",
                Title = "Full Stack .NET LMS Engineering",
                Summary = "Build production-style MVC, EF Core, Identity, SQL Server, QR attendance, and AI assistant features.",
                Description = "A graduation-project course focused on enterprise ASP.NET Core MVC architecture, secure authentication, LMS workflows, dashboards, and AI-assisted learning.",
                Category = "Software Engineering",
                Level = "Intermediate",
                AccentColor = "#0ea5e9",
                Price = 149,
                Currency = "USD",
                HeroImageUrl = "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?auto=format&fit=crop&w=1400&q=80",
                CreatedById = instructor.Id,
                Modules =
                [
                    new CourseModule
                    {
                        Title = "MVC Platform Architecture",
                        Summary = "Routing, controllers, Razor views, layout systems, and clean feature folders.",
                        SortOrder = 1,
                        Lessons =
                        [
                            new Lesson { Title = "Project blueprint", Content = "Define user roles, LMS workflows, and database boundaries.", DurationMinutes = 35, SortOrder = 1, IsPreview = true },
                            new Lesson { Title = "Identity and authorization", Content = "Secure student, instructor, and admin experiences with role-based authorization.", DurationMinutes = 45, SortOrder = 2 }
                        ]
                    },
                    new CourseModule
                    {
                        Title = "Learning Workflows",
                        Summary = "Courses, modules, lessons, assignments, quizzes, grades, and enrollment approval.",
                        SortOrder = 2,
                        Lessons =
                        [
                            new Lesson { Title = "Enrollment approval", Content = "Model pending, approved, and rejected enrollment states.", DurationMinutes = 30, SortOrder = 1 },
                            new Lesson { Title = "Assessment model", Content = "Create assignments, quizzes, submissions, attempts, and grade items.", DurationMinutes = 50, SortOrder = 2 }
                        ]
                    },
                    new CourseModule
                    {
                        Title = "Smart Campus Features",
                        Summary = "QR identity cards, QR attendance capture, and AI assistant integration.",
                        SortOrder = 3,
                        Lessons =
                        [
                            new Lesson { Title = "Student QR cards", Content = "Generate secure student QR payloads for ID and attendance.", DurationMinutes = 40, SortOrder = 1 },
                            new Lesson { Title = "OpenRouter DeepSeek assistant", Content = "Connect an LMS-aware assistant to DeepSeek through OpenRouter.", DurationMinutes = 55, SortOrder = 2 }
                        ]
                    }
                ],
                Assignments =
                [
                    new Assignment
                    {
                        Title = "Design the database ERD",
                        Brief = "Submit an ERD covering Identity users, courses, modules, lessons, assessments, enrollments, attendance, and AI conversations.",
                        DueAt = DateTimeOffset.UtcNow.AddDays(10),
                        MaxPoints = 100
                    }
                ],
                Quizzes =
                [
                    new Quiz
                    {
                        Title = "MVC and EF Core checkpoint",
                        Summary = "Short quiz on controllers, DbContext, relationships, and authorization.",
                        TimeLimitMinutes = 15,
                        Questions =
                        [
                            new QuizQuestion
                            {
                                Prompt = "Which ASP.NET Core feature is used for role-based login security?",
                                Points = 10,
                                Options =
                                [
                                    new QuizOption { Text = "Identity", IsCorrect = true },
                                    new QuizOption { Text = "SignalR", IsCorrect = false },
                                    new QuizOption { Text = "Razor Class Library only", IsCorrect = false }
                                ]
                            }
                        ]
                    }
                ],
                Announcements =
                [
                    new Announcement
                    {
                        Title = "Welcome to the DEPI LMS capstone",
                        Body = "Use the dashboard, QR ID card, attendance scanner, and AI assistant to demonstrate a modern graduation project."
                    }
                ],
                AttendanceSessions =
                [
                    new AttendanceSession
                    {
                        Title = "Kickoff lecture",
                        CreatedById = instructor.Id,
                        SessionDate = DateTimeOffset.UtcNow,
                        OpensAt = DateTimeOffset.UtcNow.AddHours(-1),
                        ClosesAt = DateTimeOffset.UtcNow.AddHours(4)
                    }
                ]
            };

            db.Courses.Add(course);
            await db.SaveChangesAsync();

            db.Enrollments.Add(new Enrollment
            {
                CourseId = course.Id,
                StudentId = student.Id,
                Status = EnrollmentStatus.Approved,
                ReviewedAt = DateTimeOffset.UtcNow,
                ReviewedById = admin.Id
            });
            db.GradeItems.Add(new GradeItem
            {
                CourseId = course.Id,
                StudentId = student.Id,
                Title = "Initial project plan",
                Category = "Assignment",
                Score = 88,
                MaxScore = 100
            });
            await db.SaveChangesAsync();
        }
    }

    private async Task EnsureRolesAsync()
    {
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Instructor, AppRoles.Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private async Task<ApplicationUser> EnsureUserAsync(string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsApproved = true,
                FullName = fullName,
                AvatarColor = role switch
                {
                    AppRoles.Admin => "#ef4444",
                    AppRoles.Instructor => "#7c3aed",
                    _ => "#0f766e"
                }
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }

    private async Task EnsureStudentProfileAsync(ApplicationUser student)
    {
        if (await db.StudentProfiles.AnyAsync(x => x.ApplicationUserId == student.Id))
        {
            return;
        }

        db.StudentProfiles.Add(new StudentProfile
        {
            ApplicationUserId = student.Id,
            StudentCode = $"DEPI-{DateTime.UtcNow:yyyy}-0001",
            Program = "Full Stack .NET",
            Level = "Graduation Project",
            EmergencyContact = "+20 100 000 0000"
        });
        await db.SaveChangesAsync();
    }

    private async Task EnsureInstructorProfileAsync(ApplicationUser instructor)
    {
        if (await db.InstructorProfiles.AnyAsync(x => x.ApplicationUserId == instructor.Id))
        {
            return;
        }

        db.InstructorProfiles.Add(new InstructorProfile
        {
            ApplicationUserId = instructor.Id,
            Department = "Software Engineering",
            Title = "Lead Instructor",
            OfficeHours = "Sunday and Tuesday, 7 PM"
        });
        await db.SaveChangesAsync();
    }
}
