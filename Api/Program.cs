using System.Text.Json.Serialization;
using Api.Data;
using Api.Models;
using Api.Services.InProgress;
using Api.Services.Published;
using Api.Services.Templates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Identity (no built-in cookie/UI; we configure cookie auth manually below)
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Auth: app cookie (default) + two Google schemes (teacher / student)
var googleClientId = builder.Configuration["Google:ClientId"] ?? "";
var googleClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "qapp.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddCookie("External-Teacher")
    .AddGoogle("Google-Teacher", options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/api/auth/google-callback-teacher";
        options.SignInScheme = "External-Teacher";
        options.SaveTokens = true;
        options.AccessType = "offline";
        options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
        // Teacher needs course management + roster + coursework grading
        options.Scope.Add("https://www.googleapis.com/auth/classroom.courses.readonly");
        options.Scope.Add("https://www.googleapis.com/auth/classroom.rosters.readonly");
        options.Scope.Add("https://www.googleapis.com/auth/classroom.coursework.students");
        options.Scope.Add("https://www.googleapis.com/auth/classroom.profile.emails");
    })
    .AddCookie("External-Student")
    .AddGoogle("Google-Student", options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/api/auth/google-callback-student";
        options.SignInScheme = "External-Student";
        options.SaveTokens = true;
        options.AccessType = "offline";
        options.ClaimActions.MapJsonKey("urn:google:picture", "picture", "url");
        // Student only needs to view their courses & their own coursework
        options.Scope.Add("https://www.googleapis.com/auth/classroom.courses.readonly");
        options.Scope.Add("https://www.googleapis.com/auth/classroom.coursework.me");
        options.Scope.Add("https://www.googleapis.com/auth/classroom.profile.emails");
    });

builder.Services.AddAuthorization();

// CORS for Vite dev server
var clientBaseUrl = builder.Configuration["Client:BaseUrl"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(clientBaseUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpClient<Api.Services.GoogleClassroomClient>();
builder.Services.AddScoped<Api.Services.ITeacherProvider, Api.Services.TeacherProvider>();
builder.Services.AddScoped<ITestTemplatesService, TestTemplatesService>();
builder.Services.AddScoped<IPublishedTestsService, PublishedTestsService>();
builder.Services.AddScoped<ITestsInProgressService, TestsInProgressService>();

var app = builder.Build();

// Apply migrations / ensure DB created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { UserRole.Student.ToString(), UserRole.Teacher.ToString() })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

app.UseCors();

// Enable WebSockets BEFORE auth so the auth middleware can authorize the upgrade.
// Restrict origins to the configured SPA so other sites can't open LSP sessions.
var wsOptions = new WebSocketOptions();
wsOptions.AllowedOrigins.Add(clientBaseUrl);
app.UseWebSockets(wsOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
