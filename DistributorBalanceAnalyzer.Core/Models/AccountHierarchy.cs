namespace DistributorBalanceAnalyzer.Core.Models;

public enum AccountLevel
{
    Distributor = 1,
    Shop = 2,
    Agent = 3
}

public class AccountHierarchy
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Level { get; set; } // 1=Distributor, 2=Shop, 3=Agent
    public string? ParentAccountId { get; set; }
}
