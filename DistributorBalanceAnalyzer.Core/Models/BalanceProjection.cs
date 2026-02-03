namespace DistributorBalanceAnalyzer.Core.Models;

public enum BalanceStatus
{
    Critical,      // < 7 days
    Warning,       // 7-30 days
    Healthy,       // > 30 days
    Increasing,    // Balance is increasing
    NoActivity,    // No sufficient data
    Depleted       // Already depleted
}

public class DepletionScenario
{
    public string ScenarioName { get; set; } = string.Empty;
    public decimal DailyBurnRate { get; set; }
    public int DaysRemaining { get; set; }
    public DateTime? DepletionDate { get; set; }
    public int DataPointDays { get; set; }
    public bool HasSufficientData { get; set; }
}

public class BalanceProjection
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public int Level { get; set; }
    public string? ParentId { get; set; }
    
    public decimal CurrentBalance { get; set; }
    
    public decimal DailyBurnRate7d { get; set; }
    public decimal DailyBurnRate30d { get; set; }
    public decimal DailyBurnRate90d { get; set; }
    
    public DepletionScenario Scenario7d { get; set; } = new();
    public DepletionScenario Scenario30d { get; set; } = new();
    public DepletionScenario Scenario90d { get; set; } = new();
    public DepletionScenario RecommendedScenario { get; set; } = new();
    
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal NetChange { get; set; }
    
    public int TransactionCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    
    public BalanceStatus Status { get; set; }
}
