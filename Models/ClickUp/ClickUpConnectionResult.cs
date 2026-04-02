namespace Allbatros.OperationsHub.Models.ClickUp;

public class ClickUpConnectionResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = "";
    public string WorkspaceName { get; set; } = "";
}
