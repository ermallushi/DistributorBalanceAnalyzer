namespace DistributorBalanceAnalyzer.Core.Models;

public class HierarchyBalanceReport
{
    public string DistributorId { get; set; } = string.Empty;
    public string DistributorName { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
    
    public BalanceProjection? DistributorBalance { get; set; }
    
    public List<BalanceProjection> ShopBalances { get; set; } = new();
    public int TotalShops { get; set; }
    public int CriticalShops { get; set; }
    public int WarningShops { get; set; }
    public int HealthyShops { get; set; }
    
    public List<BalanceProjection> AgentBalances { get; set; } = new();
    public int TotalAgents { get; set; }
    public int CriticalAgents { get; set; }
    public int WarningAgents { get; set; }
    public int HealthyAgents { get; set; }
    
    public decimal TotalBalanceAllLevels { get; set; }
    public decimal TotalDailyBurnAllLevels { get; set; }
}
