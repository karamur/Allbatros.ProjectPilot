namespace Allbatros.OperationsHub.Models.ClickUp;

public class ProjectTaskItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Status { get; set; } = "Beklemede";
    public string Priority { get; set; } = "Orta";
    public string Assignee { get; set; } = "Atanmadi";
    public DateTime? DueDate { get; set; }
    public string Source { get; set; } = "ClickUp";
    public string Summary { get; set; } = "";
}
