using Allbatros.OperationsHub.Models.ClickUp;
using System.Net.Http.Headers;
using System.Text.Json;
using Allbatros.OperationsHub.Models.ClickUp;
using Allbatros.OperationsHub.Services.Interfaces;

namespace Allbatros.OperationsHub.Services;

public class ClickUpService : IClickUpService
{
    private readonly ClickUpSettingsStore _settingsStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ClickUpService> _logger;

    public ClickUpService(ClickUpSettingsStore settingsStore, IHttpClientFactory httpClientFactory, ILogger<ClickUpService> logger)
    {
        _settingsStore = settingsStore;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<ClickUpWorkspaceSettings> GetSettingsAsync() => Task.FromResult(_settingsStore.Get());

    public Task UpdateSettingsAsync(ClickUpWorkspaceSettings settings)
    {
        _settingsStore.Update(settings);
        return Task.CompletedTask;
    }

    public async Task<ClickUpConnectionResult> TestConnectionAsync()
    {
        var settings = _settingsStore.Get();

        if (string.IsNullOrWhiteSpace(settings.ApiToken) || string.IsNullOrWhiteSpace(settings.TeamId))
        {
            return new ClickUpConnectionResult
            {
                Message = "Baglanti testi icin API token ve Team ID gerekli."
            };
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Get, settings, "team");
            using var response = await SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new ClickUpConnectionResult
                {
                    Message = $"ClickUp baglantisi basarisiz: {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            var workspaceName = FindWorkspaceName(document.RootElement, settings.TeamId) ?? settings.WorkspaceName;

            return new ClickUpConnectionResult
            {
                IsSuccess = true,
                WorkspaceName = workspaceName,
                Message = $"ClickUp baglantisi dogrulandi. Workspace: {workspaceName}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickUp connection test failed.");
            return new ClickUpConnectionResult
            {
                Message = "ClickUp baglantisi test edilirken bir hata olustu."
            };
        }
    }

    public async Task<IReadOnlyList<ProjectTaskItem>> GetTasksAsync()
    {
        var settings = _settingsStore.Get();

        if (!settings.IsConfigured)
        {
            return GetSampleTasks(settings, "Demo Veri");
        }

        try
        {
            return await GetTasksFromApiAsync(settings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClickUp API task fetch failed. Falling back to demo tasks.");
            return GetSampleTasks(settings, "Demo Veri");
        }
    }

    public async Task<ProjectTaskItem?> GetTaskByIdAsync(string taskId)
    {
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return null;
        }

        var tasks = await GetTasksAsync();
        return tasks.FirstOrDefault(task => string.Equals(task.Id, taskId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<ProjectTaskItem>> GetTasksFromApiAsync(ClickUpWorkspaceSettings settings)
    {
        using var request = CreateRequest(HttpMethod.Get, settings, $"list/{Uri.EscapeDataString(settings.ListId)}/task?include_closed=true&subtasks=true");
        using var response = await SendAsync(request);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        if (!document.RootElement.TryGetProperty("tasks", out var tasksElement) || tasksElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var tasks = new List<ProjectTaskItem>();

        foreach (var item in tasksElement.EnumerateArray())
        {
            tasks.Add(new ProjectTaskItem
            {
                Id = GetString(item, "id"),
                Title = GetString(item, "name"),
                Status = GetString(item, "status", "status", "Beklemede"),
                Priority = GetString(item, "priority", "priority", "Belirsiz"),
                Assignee = GetFirstAssignee(item),
                DueDate = GetUnixDate(item, "due_date"),
                Source = "ClickUp API"
            });
        }

        return tasks;
    }

    private IReadOnlyList<ProjectTaskItem> GetSampleTasks(ClickUpWorkspaceSettings settings, string source)
    {
        var workspaceName = string.IsNullOrWhiteSpace(settings.WorkspaceName) ? "Allbatros Global" : settings.WorkspaceName;

        IReadOnlyList<ProjectTaskItem> tasks =
        [
            new()
            {
                Id = "CLK-101",
                Title = "ClickUp API entegrasyon tasarimi",
                Status = "Devam Ediyor",
                Priority = "Yuksek",
                Assignee = "Urun Ekibi",
                DueDate = DateTime.Today.AddDays(2),
                Source = $"{workspaceName} - {source}"
            },
            new()
            {
                Id = "CLK-102",
                Title = "AI ozet kartlari icin Blazor ekraninin hazirlanmasi",
                Status = "Beklemede",
                Priority = "Orta",
                Assignee = "Frontend",
                DueDate = DateTime.Today.AddDays(5),
                Source = $"{workspaceName} - {source}"
            },
            new()
            {
                Id = "CLK-103",
                Title = "Webhook olaylarinin servis katmanina alinmasi",
                Status = "Riskli",
                Priority = "Kritik",
                Assignee = "Backend",
                DueDate = DateTime.Today.AddDays(1),
                Source = $"{workspaceName} - {source}"
            }
        ];

        return tasks;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var client = _httpClientFactory.CreateClient(nameof(ClickUpService));
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, ClickUpWorkspaceSettings settings, string relativePath)
    {
        var baseUrl = string.IsNullOrWhiteSpace(settings.ApiBaseUrl)
            ? "https://api.clickup.com/api/v2/"
            : $"{settings.ApiBaseUrl.TrimEnd('/')}/";

        var request = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), relativePath));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(settings.ApiToken))
        {
            request.Headers.TryAddWithoutValidation("Authorization", settings.ApiToken);
        }

        return request;
    }

    private static string? FindWorkspaceName(JsonElement root, string teamId)
    {
        if (!root.TryGetProperty("teams", out var teamsElement) || teamsElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var team in teamsElement.EnumerateArray())
        {
            if (GetString(team, "id") == teamId)
            {
                return GetString(team, "name");
            }
        }

        return null;
    }

    private static string GetFirstAssignee(JsonElement item)
    {
        if (!item.TryGetProperty("assignees", out var assigneesElement) || assigneesElement.ValueKind != JsonValueKind.Array)
        {
            return "Atanmadi";
        }

        foreach (var assignee in assigneesElement.EnumerateArray())
        {
            var username = GetString(assignee, "username");
            if (!string.IsNullOrWhiteSpace(username))
            {
                return username;
            }

            var email = GetString(assignee, "email");
            if (!string.IsNullOrWhiteSpace(email))
            {
                return email;
            }
        }

        return "Atanmadi";
    }

    private static DateTime? GetUnixDate(JsonElement item, string propertyName)
    {
        var rawValue = GetString(item, propertyName);
        return long.TryParse(rawValue, out var milliseconds)
            ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).LocalDateTime
            : null;
    }

    private static string GetString(JsonElement item, string propertyName, string fallback = "")
    {
        if (!item.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.Number => value.ToString(),
            _ => fallback
        };
    }

    private static string GetString(JsonElement item, string parentPropertyName, string childPropertyName, string fallback)
    {
        if (!item.TryGetProperty(parentPropertyName, out var parent) || parent.ValueKind != JsonValueKind.Object)
        {
            return fallback;
        }

        return GetString(parent, childPropertyName, fallback);
    }
}
