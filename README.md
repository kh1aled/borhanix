# DEPI LMS

ASP.NET Core MVC + EF Core + SQL Server + Identity education platform for a DEPI graduation project.

## Run

```powershell
cd "C:\Users\momen\Documents\Codex\2026-06-17\i-wanna-you-to-make-a\outputs\DEPI-LMS"
dotnet restore
dotnet run --urls http://localhost:5041
```

Open:

```text
http://localhost:5041
```

## Demo accounts

```text
Admin:      admin@depi.edu / Admin@12345
Instructor: instructor@depi.edu / Instructor@12345
Student:    student@depi.edu / Student@12345
```

New registrations are pending by default. Login as admin, open `Admin > Users`, then approve the account.

## AI setup

The AI assistant and AI quiz generation use OpenRouter's OpenAI-compatible chat endpoint with a DeepSeek model.

```powershell
dotnet user-secrets set "OpenRouter:ApiKey" "YOUR_OPENROUTER_KEY"
dotnet user-secrets set "OpenRouter:Model" "deepseek/deepseek-chat"
```

The model can be changed in `appsettings.json`.

## Implemented features

- Student, Instructor, Admin roles with ASP.NET Core Identity.
- Admin approval required for new accounts.
- Full LMS structure: Courses -> Modules -> Lessons -> Assignments, Quizzes, Grades.
- Enrollment request and instructor/admin approval.
- Student QR ID card and instructor QR attendance scanner.
- Student profile page with editable info and profile photo upload.
- Admin user control: approve, edit, change roles, delete users.
- Instructor/admin course control: create, edit, delete; instructors only manage their own courses.
- Lesson video upload or video URL support.
- Assignment submission with file upload/URL and instructor grading.
- Quiz taking, auto-scoring, grade recording, and instructor attempt review.
- AI-generated quizzes per lesson through OpenRouter/DeepSeek.
- Dark mode toggle with local browser preference.

## Notes

- Development database: SQL Server LocalDB database `DepiLmsDbV2`.
- Uploaded profile photos, videos, and assignment files are stored under `wwwroot/uploads`.
- This project uses `EnsureCreated` for easy graduation-project setup. For production, replace it with EF Core migrations.
