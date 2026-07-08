using DepiLms.Data;
using DepiLms.Models;
using DepiLms.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";
    options.Cookie.Name = "DepiLms.Identity";
});

builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<IOpenRouterAiService, OpenRouterAiService>();
builder.Services.AddScoped<IAiQuizGeneratorService, AiQuizGeneratorService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<ICourseImageUploadService, CourseImageUploadService>();
builder.Services.AddScoped<IVideoDurationService, VideoDurationService>();
builder.Services.AddScoped<IAssignmentAccessService, AssignmentAccessService>();
builder.Services.AddScoped<ILessonProgressService, LessonProgressService>();
builder.Services.AddScoped<ICourseCompletionService, CourseCompletionService>();
builder.Services.AddScoped<ICertificateGenerationService, CertificateGenerationService>();
builder.Services.AddScoped<PlatformSeeder>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<PlatformSeeder>();
    await seeder.SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".mp4"] = "video/mp4";
contentTypeProvider.Mappings[".m4v"] = "video/mp4";
contentTypeProvider.Mappings[".mov"] = "video/quicktime";
contentTypeProvider.Mappings[".webm"] = "video/webm";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
