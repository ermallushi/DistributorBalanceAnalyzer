namespace DistributorBalanceAnalyzer.Core.Models;

/// <summary>
/// Represents balance data aggregated by hierarchy level
/// </summary>
public class HierarchyLevelBalance
{
    public int Level { get; set; }
    public string LevelDescription { get; set; } = string.Empty;
    public int EntityCount { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public List<string> EntityIds { get; set; } = new List<string>();
}
