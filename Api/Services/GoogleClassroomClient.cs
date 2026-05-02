using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class GoogleClassroomClient
{
    private readonly HttpClient _http;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleClassroomClient> _logger;

    public GoogleClassroomClient(
        HttpClient http,
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IConfiguration config,
        ILogger<GoogleClassroomClient> logger)
    {
        _http = http;
        _userManager = userManager;
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<List<CourseInfo>> GetTeacherCoursesAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var token = await GetValidAccessTokenAsync(user, ct);
        if (token is null)
        {
            throw new InvalidOperationException("No Google access token available for this user.");
        }

        var url = "https://classroom.googleapis.com/v1/courses?teacherId=me&courseStates=ACTIVE";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await _http.SendAsync(req, ct);
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Refresh once and retry
            token = await RefreshAccessTokenAsync(user, ct);

            if (token is null)
            {
                throw new InvalidOperationException("Failed to refresh Google access token.");
            }

            using var req2 = new HttpRequestMessage(HttpMethod.Get, url);
            req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var resp2 = await _http.SendAsync(req2, ct);
            return await ParseCoursesAsync(resp2, ct);
        }

        return await ParseCoursesAsync(resp, ct);
    }

    private static async Task<List<CourseInfo>> ParseCoursesAsync(HttpResponseMessage resp, CancellationToken ct)
    {
        resp.EnsureSuccessStatusCode();
        var payload = await resp.Content.ReadFromJsonAsync<CoursesResponse>(cancellationToken: ct);
        return payload?.Courses ?? new List<CourseInfo>();
    }

    public async Task<CourseWorkInfo> CreateCourseWorkAsync(
        ApplicationUser user,
        string courseId,
        string title,
        string? description,
        string linkUrl,
        DateTimeOffset closesAt,
        int maxPoints,
        CancellationToken ct = default)
    {
        var url = $"https://classroom.googleapis.com/v1/courses/{Uri.EscapeDataString(courseId)}/courseWork";

        var dueUtc = closesAt.ToUniversalTime();
        var body = new
        {
            title,
            description,
            workType = "ASSIGNMENT",
            state = "PUBLISHED",
            maxPoints = (double)maxPoints,
            materials = new[]
            {
                new { link = new { url = linkUrl } }
            },
            dueDate = new { year = dueUtc.Year, month = dueUtc.Month, day = dueUtc.Day },
            dueTime = new { hours = dueUtc.Hour, minutes = dueUtc.Minute }
        };

        async Task<HttpResponseMessage> PostAsync(string token)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _http.SendAsync(req, ct);
        }

        var token = await GetValidAccessTokenAsync(user, ct)
            ?? throw new InvalidOperationException("No Google access token available for this user.");

        var resp = await PostAsync(token);
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            resp.Dispose();
            token = await RefreshAccessTokenAsync(user, ct)
                ?? throw new InvalidOperationException("Failed to refresh Google access token.");
            resp = await PostAsync(token);
        }

        using (resp)
        {
            if (!resp.IsSuccessStatusCode)
            {
                var detail = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Classroom courseWork create failed: {Status} {Body}", resp.StatusCode, detail);
                throw new HttpRequestException($"Classroom rejected courseWork create ({(int)resp.StatusCode}): {detail}");
            }
            var payload = await resp.Content.ReadFromJsonAsync<CourseWorkInfo>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Empty courseWork response.");
            return payload;
        }
    }

    private async Task<string?> GetValidAccessTokenAsync(ApplicationUser user, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(user.GoogleAccessToken)
            && (user.GoogleTokenExpiresAt is null || user.GoogleTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1)))
        {
            return user.GoogleAccessToken;
        }
        return await RefreshAccessTokenAsync(user, ct);
    }

    private async Task<string?> RefreshAccessTokenAsync(ApplicationUser user, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(user.GoogleRefreshToken))
        {
            return null;
        }

        var clientId = _config["Google:ClientId"];
        var clientSecret = _config["Google:ClientSecret"];
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return null;
        }

        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("client_id", clientId),
            new KeyValuePair<string,string>("client_secret", clientSecret),
            new KeyValuePair<string,string>("refresh_token", user.GoogleRefreshToken),
            new KeyValuePair<string,string>("grant_type", "refresh_token"),
        });

        using var resp = await _http.PostAsync("https://oauth2.googleapis.com/token", content, ct);
        
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google token refresh failed: {Status}", resp.StatusCode);
            return null;
        }

        var token = await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);

        if (token is null || string.IsNullOrEmpty(token.AccessToken))
        {
            return null;
        }

        user.GoogleAccessToken = token.AccessToken;
        user.GoogleTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn > 0 ? token.ExpiresIn : 3500);
        await _userManager.UpdateAsync(user);

        await _db.SaveChangesAsync(ct);

        return token.AccessToken;
    }

    public class CourseInfo
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("section")] public string? Section { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
    }

    public class CourseWorkInfo
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("alternateLink")] public string? AlternateLink { get; set; }
    }

    private class CoursesResponse
    {
        [JsonPropertyName("courses")] public List<CourseInfo>? Courses { get; set; }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    }
}
