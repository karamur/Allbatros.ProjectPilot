using Allbatros.OperationsHub.Models.ClickUp;

namespace Allbatros.OperationsHub.Services;

public class ClickUpSettingsStore
{
    private ClickUpWorkspaceSettings _settings;

    public ClickUpSettingsStore(IConfiguration configuration)
    {
        var settings = new ClickUpWorkspaceSettings();
        configuration.GetSection("ClickUp").Bind(settings);
        _settings = settings;
    }

    public ClickUpWorkspaceSettings Get() => _settings.Clone();

    public void Update(ClickUpWorkspaceSettings settings)
    {
        _settings = new ClickUpWorkspaceSettings
        {
            WorkspaceName = string.IsNullOrWhiteSpace(settings.WorkspaceName) ? "Allbatros Global" : settings.WorkspaceName.Trim(),
            ApiBaseUrl = string.IsNullOrWhiteSpace(settings.ApiBaseUrl) ? "https://api.clickup.com/api/v2" : settings.ApiBaseUrl.Trim(),
            ApiToken = settings.ApiToken.Trim(),
            TeamId = settings.TeamId.Trim(),
            ListId = settings.ListId.Trim()
        };
    }
}
