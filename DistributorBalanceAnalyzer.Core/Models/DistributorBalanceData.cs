namespace DistributorBalanceAnalyzer.Core.Models;

/// <summary>
/// Aggregated balance transaction data from the database
/// </summary>
public class DistributorBalanceData
{
    public string DistributorId { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public DateTime FirstTransactionDate { get; set; }
    public DateTime LastTransactionDate { get; set; }
    public decimal CurrentBalance { get; set; }
    
    // Totals
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    
    // 7-day window
    public decimal Debits7d { get; set; }
    public DateTime? FirstDebit7d { get; set; }
    public DateTime? LastDebit7d { get; set; }
    
    // 30-day window
    public decimal Debits30d { get; set; }
    public DateTime? FirstDebit30d { get; set; }
    public DateTime? LastDebit30d { get; set; }
    
    // 90-day window
    public decimal Debits90d { get; set; }
    public DateTime? FirstDebit90d { get; set; }
    public DateTime? LastDebit90d { get; set; }
}
