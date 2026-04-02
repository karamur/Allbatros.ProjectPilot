namespace Allbatros.OperationsHub.Models.ClickUp;

public class ClickUpWorkspaceSettings
{
    public string WorkspaceName { get; set; } = "Allbatros Global";
    public string ApiBaseUrl { get; set; } = "https://api.clickup.com/api/v2";
    public string ApiToken { get; set; } = "";
    public string TeamId { get; set; } = "";
    public string ListId { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiToken) &&
        !string.IsNullOrWhiteSpace(TeamId) &&
        !string.IsNullOrWhiteSpace(ListId);

    public ClickUpWorkspaceSettings Clone() => new()
    {
        WorkspaceName = WorkspaceName,
        ApiBaseUrl = ApiBaseUrl,
        ApiToken = ApiToken,
        TeamId = TeamId,
        ListId = ListId
    };
}
