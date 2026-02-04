namespace DistributorBalanceAnalyzer.Core.Models;

/// <summary>
/// Complete balance projection analysis with multiple scenarios
/// </summary>
public class DistributorBalanceProjection
{
    public string DistributorId { get; set; } = string.Empty;
    public string DistributorName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    
    // Multiple burn rate scenarios
    public decimal DailyBurnRate7d { get; set; }
    public decimal DailyBurnRate30d { get; set; }
    public decimal DailyBurnRate90d { get; set; }
    public decimal DailyBurnRateAllTime { get; set; }
    
    // Projections based on different scenarios
    public DepletionScenario Scenario7d { get; set; } = new();
    public DepletionScenario Scenario30d { get; set; } = new();
    public DepletionScenario Scenario90d { get; set; } = new();
    public DepletionScenario ScenarioAllTime { get; set; } = new();
    
    // Recommended scenario (weighted average)
    public DepletionScenario RecommendedScenario { get; set; } = new();
    
    // Metrics
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal NetChange { get; set; }
    public int TransactionCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public DateTime GeneratedAt { get; set; }
}
