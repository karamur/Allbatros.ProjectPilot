using Allbatros.OperationsHub.Models.AI;
using Allbatros.OperationsHub.Models.ClickUp;
using Allbatros.OperationsHub.Services.Interfaces;

namespace Allbatros.OperationsHub.Services;

public class AIService : IAIService
{
    public Task<string> BuildTaskSummaryAsync(ProjectTaskItem task)
    {
        var dueText = task.DueDate.HasValue
            ? $"Teslim tarihi {task.DueDate:dd.MM.yyyy}."
            : "Teslim tarihi henuz belirlenmemis.";

        var summary = $"{task.Title} gorevi {task.Assignee} sorumlulugunda. Durum: {task.Status}. Oncelik: {task.Priority}. {dueText}";
        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<AIInsight>> GenerateInsightsAsync(IEnumerable<ProjectTaskItem> tasks)
    {
        var taskList = tasks.ToList();
        var insights = new List<AIInsight>();

        var riskyTasks = taskList
            .Where(t => t.Priority is "Kritik" or "Yuksek" && t.DueDate.HasValue && t.DueDate.Value.Date <= DateTime.Today.AddDays(2))
            .ToList();

        if (riskyTasks.Count > 0)
        {
            insights.Add(new AIInsight
            {
                Title = "Yaklasan kritik teslimler",
                Category = "Takvim",
                Severity = "High",
                Description = $"{riskyTasks.Count} gorev yakin tarihte teslim edilmeli. Ilk odak alani: {riskyTasks[0].Title}.",
                ActionLabel = "Tasklari Ac"
            });
        }

        var waitingTasks = taskList.Count(t => t.Status == "Beklemede");
        if (waitingTasks > 0)
        {
            insights.Add(new AIInsight
            {
                Title = "Bekleyen gorev yogunlugu",
                Category = "Operasyon",
                Severity = "Medium",
                Description = $"{waitingTasks} gorev halen beklemede. Onceliklendirme toplantisi onerilir.",
                ActionLabel = "Onceliklendir"
            });
        }

        if (insights.Count == 0)
        {
            insights.Add(new AIInsight
            {
                Title = "Denge durumu iyi",
                Category = "Genel",
                Severity = "Info",
                Description = "Su an gorunen task dagilimi dengeli. Yeni entegrasyon adimlarina gecilebilir.",
                ActionLabel = "Detay Goster"
            });
        }

        return Task.FromResult<IReadOnlyList<AIInsight>>(insights);
    }
}
