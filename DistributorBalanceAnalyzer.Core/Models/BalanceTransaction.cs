namespace DistributorBalanceAnalyzer.Core.Models;

/// <summary>
/// Represents a single balance transaction from the database
/// </summary>
public class BalanceTransaction
{
    public long TransactionId { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public decimal OldBalance { get; set; }
    public decimal NewBalance { get; set; }
    public string Event { get; set; } = string.Empty;
    public string BalanceEffect { get; set; } = string.Empty;
}
