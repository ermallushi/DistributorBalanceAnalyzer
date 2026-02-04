namespace DistributorBalanceAnalyzer.Core.Models;

/// <summary>
/// Represents the status of a distributor's balance based on projected depletion timeline
/// </summary>
public enum BalanceStatus
{
    /// <summary>
    /// Balance will be depleted in less than 7 days - immediate action required
    /// </summary>
    Critical,
    
    /// <summary>
    /// Balance will be depleted in 7-30 days - action needed soon
    /// </summary>
    Warning,
    
    /// <summary>
    /// Balance will be depleted in 30-90 days - monitor closely
    /// </summary>
    Moderate,
    
    /// <summary>
    /// Balance will last more than 90 days - healthy status
    /// </summary>
    Healthy,
    
    /// <summary>
    /// Balance is growing (credits exceed debits)
    /// </summary>
    Increasing,
    
    /// <summary>
    /// No recent transaction activity detected
    /// </summary>
    NoActivity,
    
    /// <summary>
    /// Balance is at or below zero
    /// </summary>
    Depleted
}
