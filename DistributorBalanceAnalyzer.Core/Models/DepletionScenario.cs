namespace DistributorBalanceAnalyzer.Core.Models;

/// <summary>
/// Represents a balance depletion scenario based on a specific time window
/// </summary>
public class DepletionScenario
{
    public string ScenarioName { get; set; } = string.Empty;
    public decimal DailyBurnRate { get; set; }
    public int DaysRemaining { get; set; }
    public DateTime? DepletionDate { get; set; }
    public int DataPointDays { get; set; }
    public bool HasSufficientData { get; set; }
    public BalanceStatus Status { get; set; }
}
