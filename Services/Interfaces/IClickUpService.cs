using Allbatros.OperationsHub.Models.ClickUp;

namespace Allbatros.OperationsHub.Services.Interfaces;

public interface IClickUpService
{
    Task<ClickUpWorkspaceSettings> GetSettingsAsync();
    Task UpdateSettingsAsync(ClickUpWorkspaceSettings settings);
    Task<ClickUpConnectionResult> TestConnectionAsync();
    Task<IReadOnlyList<ProjectTaskItem>> GetTasksAsync();
    Task<ProjectTaskItem?> GetTaskByIdAsync(string taskId);
}
