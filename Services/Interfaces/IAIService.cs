using Allbatros.OperationsHub.Models.AI;
using Allbatros.OperationsHub.Models.ClickUp;

namespace Allbatros.OperationsHub.Services.Interfaces;

public interface IAIService
{
    Task<string> BuildTaskSummaryAsync(ProjectTaskItem task);
    Task<IReadOnlyList<AIInsight>> GenerateInsightsAsync(IEnumerable<ProjectTaskItem> tasks);
}
